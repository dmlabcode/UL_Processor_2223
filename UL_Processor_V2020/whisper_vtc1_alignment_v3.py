import os
import re
import argparse
import pandas as pd
from datetime import datetime, timedelta
import sys

sys.stdout.reconfigure(encoding="utf-8")


# =========================================================
# TIME HELPERS
# =========================================================

def add_seconds_to_hms(hms, seconds):
    base = datetime.strptime(hms, "%H:%M:%S")
    return (base + timedelta(seconds=float(seconds))).time().strftime("%H:%M:%S")


def hms_to_seconds(hms):
    if pd.isna(hms):
        return None

    parts = str(hms).strip().split(":")

    if len(parts) == 3:
        h, m, s = parts
    elif len(parts) == 2:
        h, m = parts
        s = 0
    else:
        return None

    return int(h) * 3600 + int(m) * 60 + int(s)


# =========================================================
# RTTM PARSER
# =========================================================

def parse_rttm(rttm_path):
    with open(rttm_path, "r") as f:
        lines = f.readlines()

    data = []

    for line in lines:
        parts = line.split()
        if len(parts) < 8:
            continue

        file_id = parts[1]
        start = float(parts[3])
        duration = float(parts[4])
        speaker = parts[7]

        if speaker == "SPEECH":
            continue

        data.append([file_id, start, duration, speaker])

    return pd.DataFrame(
        data,
        columns=["file_id", "start_sec", "duration", "speaker"]
    )


# =========================================================
# WHISPER CLEANING
# =========================================================

def filter_whisper_loops(df):
    sentences = df["sentence"].fillna("").astype(str).tolist()
    keep = []

    for i, s in enumerate(sentences):

        if s == "":
            keep.append(df.index[i])
            continue

        # remove A A A loops
        if i >= 2 and s == sentences[i - 1] == sentences[i - 2]:
            continue

        keep.append(df.index[i])

    return df.loc[keep].copy()


# =========================================================
# SONY ID
# =========================================================

def extract_sony_id(filename):
    match = re.match(r"(\d{2,3})_", os.path.basename(filename))
    if not match:
        return None
    return match.group(1).zfill(3)


# =========================================================
# MAIN PROCESS
# =========================================================

def process_folder(csv_folder, rttm_file, output_folder, mapping_file=None , use_alt_labels=False):

    os.makedirs(output_folder, exist_ok=True)

    # -------------------------
    # LOAD MAPPING (OPTIONAL)
    # -------------------------
    mapping_lookup = {}

    if mapping_file:
        mapping_df = pd.read_csv(mapping_file)
        mapping_df = mapping_df[mapping_df["STATUS"] == "PRESENT"].copy()

        mapping_df["sony_id"] = (
            mapping_df["SONY ID"]
            .astype(int)
            .astype(str)
            .str.zfill(3)
        )

        mapping_lookup = {
            row["sony_id"]: row
            for _, row in mapping_df.iterrows()
        }

        print(f"Loaded mapping file: {len(mapping_lookup)} entries")
    else:
        print("No mapping file provided — running in fallback mode")

    # -------------------------
    # LOAD RTTM
    # -------------------------
    rttm_df = parse_rttm(rttm_file)

    # -------------------------
    # FILE LIST
    # -------------------------
    csv_files = [
        os.path.join(csv_folder, f)
        for f in os.listdir(csv_folder)
        if f.lower().endswith(".csv")
    ]

    print(f"Found {len(csv_files)} CSV files")

    # =====================================================
    # PROCESS FILES
    # =====================================================
    for csv_path in csv_files:

        print("\nProcessing:", csv_path)

        df = pd.read_csv(csv_path)

        # -------------------------
        # ID MATCHING
        # -------------------------
        sony_id = extract_sony_id(csv_path)
        mapping_row = mapping_lookup.get(sony_id, None)

        # -------------------------
        # WHISPER CLEAN
        # -------------------------
        df = filter_whisper_loops(df)

        # -------------------------
        # TIME ALIGNMENT
        # -------------------------
        audio_on = None

        if "time_start" in df.columns:
            print("Using existing time_start column")
            df["time_start_sec"] = df["time_start"].apply(hms_to_seconds)

        elif mapping_row is not None and "AUDIO_ON" in mapping_row.index and pd.notna(mapping_row["AUDIO_ON"]):

            audio_on = str(mapping_row["AUDIO_ON"]).strip()
            print(f"Using AUDIO_ON = {audio_on}")

            df["time_start"] = df["start_sec"].apply(
                lambda x: add_seconds_to_hms(audio_on, x)
            )
            df["time_start_sec"] = df["time_start"].apply(hms_to_seconds)

        else:
            print("No time_start or AUDIO_ON — defaulting to 00:00:00")

            df["time_start"] = df["start_sec"].apply(
                lambda x: add_seconds_to_hms("00:00:00", x)
            )
            df["time_start_sec"] = df["start_sec"]

        # -------------------------
        # VEST FILTER (OPTIONAL)
        # -------------------------
        vest_applied = False

        if mapping_row is not None:

            vest_on_raw = mapping_row.get("VEST ON Starts", None)
            vest_off_raw = mapping_row.get("VEST OFF Expires", None)

            vest_on = hms_to_seconds(vest_on_raw)
            vest_off = hms_to_seconds(vest_off_raw)

            if vest_on is not None and vest_off is not None:

                before = len(df)

                df = df[
                    (df["time_start_sec"] >= vest_on) &
                    (df["time_start_sec"] <= vest_off)
                ].copy()

                vest_applied = True
                print(f"Vest filter applied: {before} → {len(df)} rows")
            else:
                print("Vest times invalid — skipping vest filter")
        else:
            print("No mapping row — skipping vest filter")

        # -------------------------
        # RTTM FILTER
        # -------------------------
        if sony_id:
            file_rttm = rttm_df[
                rttm_df["file_id"].astype(str).str.contains(sony_id)
            ].copy()
        else:
            file_rttm = pd.DataFrame()

        # -------------------------
        # SPEAKER ALIGNMENT
        # -------------------------
        speakers = []

        for _, row in df.iterrows():

            start = float(row["start_sec"])
            end = float(row["end_sec"])

            overlaps = file_rttm[
                (file_rttm["start_sec"] < end) &
                ((file_rttm["start_sec"] + file_rttm["duration"]) > start)
            ].copy()

            if overlaps.empty:
                speakers.append("Unidentified_Speaker")
                continue

            overlaps["overlap_start"] = overlaps["start_sec"].clip(lower=start)
            overlaps["overlap_end"] = (overlaps["start_sec"] + overlaps["duration"]).clip(upper=end)
            overlaps["overlap_duration"] = overlaps["overlap_end"] - overlaps["overlap_start"]

            overlaps = overlaps[overlaps["overlap_duration"] > 0.1]

            if overlaps.empty:
                speakers.append("Unidentified_Speaker")
                continue

            scores = overlaps.groupby("speaker")["overlap_duration"].sum()
            speakers.append(scores.idxmax())

        df["speaker"] = speakers

        # -------------------------
        # OUTPUT
        # -------------------------
        final_df = df[["start_sec", "end_sec", "time_start", "sentence", "speaker"]].copy()

        out_path = os.path.join(
            output_folder,
            os.path.basename(csv_path).replace(".csv", "_ALICE.csv")
        )

        final_df.to_csv(out_path, index=False)

        print("Saved:", out_path)


# =========================================================
# ENTRY POINT
# =========================================================

if __name__ == "__main__":

    parser = argparse.ArgumentParser()

    parser.add_argument("csv_folder")
    parser.add_argument("rttm_file")
    parser.add_argument("output_folder")
    parser.add_argument("--mapping_file", default=None)
    parser.add_argument("--alt-labels", action="store_true", help="Use alternate speaker labels (Teacher, Male_Adult, Other_Child, Main_Child)")
    
    args = parser.parse_args()

    process_folder(
        args.csv_folder,
        args.rttm_file,
        args.output_folder,
        args.mapping_file, args.alt_labels
    )