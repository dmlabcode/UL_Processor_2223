import csv
import sys
from pathlib import Path


def time_to_seconds(time_str):
    """
    Converts HH:MM:SS or H:MM:SS to total seconds.
    """
    parts = time_str.strip().split(":")

    if len(parts) != 3:
        raise ValueError(f"Invalid time format: {time_str}")

    hours, minutes, seconds = map(int, parts)

    return hours * 3600 + minutes * 60 + seconds


def overlap_duration(a_start, a_end, b_start, b_end):
    """
    Returns overlap duration between two time intervals.
    """
    overlap_start = max(a_start, b_start)
    overlap_end = min(a_end, b_end)

    return max(0, overlap_end - overlap_start)


def detect_file_type(filepath):
    """
    Detects:
    - CSV
    - RTTM / RTTM.TXT
    """
    path = Path(filepath)

    if path.suffix.lower() == ".csv":
        return "csv"

    if (
        path.suffix.lower() == ".rttm"
        or filepath.lower().endswith(".rttm.txt")
    ):
        return "rttm"

    raise ValueError(f"Unsupported file type: {filepath}")


def parse_vtc_row(row):
    """
    Supports TWO VTC formats:

    FORMAT A (time strings):
        row[10] = start_time
        row[11] = end_time

    FORMAT B (seconds):
        row[3] = start_sec
        row[4] = duration_sec
    """

    # Speaker type always expected here
    speaker_type = row[7].strip()

    if speaker_type not in {"KCHI", "FEM"}:
        return None

    # -----------------------------
    # FORMAT A: HH:MM:SS columns
    # -----------------------------
    if len(row) > 11 and ":" in row[10] and ":" in row[11]:

        start_sec = time_to_seconds(row[10].strip())
        end_sec = time_to_seconds(row[11].strip())

    # -----------------------------
    # FORMAT B: numeric seconds
    # row[3] = start_sec
    # row[4] = duration_sec
    # -----------------------------
    else:
        start_sec = float(row[3])
        duration_sec = float(row[4])
        end_sec = start_sec + duration_sec

    return {
        "speaker_type": speaker_type,
        "start_sec": start_sec,
        "end_sec": end_sec
    }


def load_vtc(vtc_path):
    """
    Loads VTC rows from:
    - CSV
    - RTTM
    - RTTM.TXT
    """

    file_type = detect_file_type(vtc_path)

    vtc_rows = []

    with open(vtc_path, "r", encoding="utf-8") as f:

        # --------------------------------
        # CSV
        # --------------------------------
        if file_type == "csv":

            reader = csv.reader(f)

            for row in reader:
                try:
                    if len(row) < 8:
                        continue

                    parsed = parse_vtc_row(row)

                    if parsed:
                        vtc_rows.append(parsed)

                except Exception as e:
                    print(f"Skipping malformed CSV row:\n{row}")
                    print(f"Reason: {e}")

        # --------------------------------
        # RTTM / space separated
        # --------------------------------
        else:

            for line in f:
                try:
                    line = line.strip()

                    if not line:
                        continue

                    # Split on ANY whitespace
                    row = line.split()

                    if len(row) < 8:
                        continue

                    parsed = parse_vtc_row(row)

                    if parsed:
                        vtc_rows.append(parsed)

                except Exception as e:
                    print(f"Skipping malformed RTTM row:\n{line}")
                    print(f"Reason: {e}")

    return vtc_rows


def get_whisper_times(row):
    """
    Supports TWO Whisper formats:

    FORMAT A:
        time_start
        time_end

    FORMAT B:
        start_sec
        end_sec
    """

    # --------------------------------
    # FORMAT A: HH:MM:SS
    # --------------------------------
    if "time_start" in row and "time_end" in row:

        whisper_start = time_to_seconds(row["time_start"])
        whisper_end = time_to_seconds(row["time_end"])

    # --------------------------------
    # FORMAT B: numeric seconds
    # --------------------------------
    elif "start_sec" in row and "end_sec" in row:

        whisper_start = float(row["start_sec"])
        whisper_end = float(row["end_sec"])

    else:
        raise ValueError(
            "Whisper CSV must contain either:\n"
            "- time_start/time_end\n"
            "OR\n"
            "- start_sec/end_sec"
        )

    return whisper_start, whisper_end


def process_whisper(whisper_csv_path, vtc_rows, output_csv_path):
    """
    Matches each Whisper row to best overlapping VTC row.
    """

    with open(whisper_csv_path, "r", encoding="utf-8") as infile, \
         open(output_csv_path, "w", newline="", encoding="utf-8") as outfile:

        reader = csv.DictReader(infile)

        fieldnames = reader.fieldnames + ["matched_speaker_type"]

        writer = csv.DictWriter(outfile, fieldnames=fieldnames)

        writer.writeheader()

        for row in reader:
            try:

                whisper_start, whisper_end = get_whisper_times(row)

                best_match = None
                best_overlap = 0

                for vtc in vtc_rows:

                    overlap = overlap_duration(
                        whisper_start,
                        whisper_end,
                        vtc["start_sec"],
                        vtc["end_sec"]
                    )

                    if overlap > best_overlap:
                        best_overlap = overlap
                        best_match = vtc

                if best_match:
                    row["matched_speaker_type"] = best_match["speaker_type"]
                else:
                    row["matched_speaker_type"] = "NO_MATCH"

                writer.writerow(row)

            except Exception as e:
                print(f"Error processing Whisper row:\n{row}")
                print(f"Reason: {e}")


def main():
    """
    Usage examples:

    python vtcwhisper.py whisper.csv vtc.csv output.csv

    python vtcwhisper.py whisper.csv vtc.rttm output.csv

    python vtcwhisper.py whisper.csv vtc.rttm.txt output.csv
    """

    if len(sys.argv) != 4:

        print(
            "Usage:\n"
            "python vtcwhisper.py whisper.csv vtc_file output.csv"
        )

        sys.exit(1)

    whisper_csv = sys.argv[1]
    vtc_file = sys.argv[2]
    output_csv = sys.argv[3]

    print("Loading VTC file...")

    vtc_rows = load_vtc(vtc_file)

    print(f"Loaded {len(vtc_rows)} KCHI/FEM VTC rows")

    print("Processing Whisper CSV...")

    process_whisper(
        whisper_csv,
        vtc_rows,
        output_csv
    )

    print(f"\nDone.\nOutput saved to:\n{output_csv}")


if __name__ == "__main__":
    main()