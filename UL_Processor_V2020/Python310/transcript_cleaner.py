from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

import pandas as pd
from tqdm import tqdm


SUPPORTED_EXTENSIONS = {".csv", ".tsv", ".xlsx", ".xls"}
DEFAULT_BATCH_OUTPUT_FOLDER = "Cleaned_Transcripts"


def clean_transcription(text) -> str:
    if pd.isna(text):
        return ""

    text = " ".join(str(text).split())

    terminal_punctuation = ""
    match = re.search(r"([.!?]+)$", text)

    if match:
        punctuation = match.group(1)

        if "?" in punctuation:
            terminal_punctuation = "?"
        elif "!" in punctuation:
            terminal_punctuation = "!"
        else:
            terminal_punctuation = "."

        text = text[: match.start()]

    text = re.sub(
        r"[^a-zA-ZñÑáéíóúÁÉÍÓÚüÜ\s]",
        "",
        text,
    )

    text = " ".join(text.split())

    if not text:
        return ""

    return text + terminal_punctuation


def identical_repeat_flags(sentences: pd.Series, keep: int = 2) -> pd.Series:
    groups = sentences.ne(sentences.shift()).cumsum()
    return sentences.groupby(groups).cumcount().ge(keep)


def ab_repeat_flags(
    sentences: pd.Series,
    keep_pairs: int = 2,
    min_pairs_to_trigger: int = 3,
) -> pd.Series:
    values = sentences.fillna("").astype(str).tolist()
    n = len(values)
    flags = [False] * n

    i = 0

    while i < n - 1:
        a = values[i]
        b = values[i + 1]

        if not a or not b:
            i += 1
            continue

        if a == b:
            i += 1
            continue

        j = i + 2

        while j < n and values[j] == values[j - 2]:
            j += 1

        pattern_length = j - i
        complete_pairs = pattern_length // 2

        if complete_pairs >= min_pairs_to_trigger:
            first_remove = i + (keep_pairs * 2)

            for k in range(first_remove, j):
                flags[k] = True

            i = j
        else:
            i += 1

    return pd.Series(flags, index=sentences.index)


def is_extreme_stretch_hallucination(
    text,
    min_token_length: int = 20,
    min_identical_run: int = 12,
) -> bool:
    if pd.isna(text):
        return False

    body = re.sub(r"[.!?]+$", "", str(text)).strip()

    if not body:
        return False

    if any(ch.isspace() for ch in body):
        return False

    if len(body) < min_token_length:
        return False

    longest_run = max(
        (len(match.group(0)) for match in re.finditer(r"(.)\1*", body.casefold())),
        default=0,
    )

    return longest_run >= min_identical_run


def read_transcript(path: Path) -> pd.DataFrame:
    ext = path.suffix.lower()

    if ext == ".csv":
        return pd.read_csv(path)
    if ext == ".tsv":
        return pd.read_csv(path, sep="\t")
    if ext in {".xlsx", ".xls"}:
        return pd.read_excel(path)

    raise ValueError(
        f"Unsupported input format '{ext}'. "
        f"Supported formats: {', '.join(sorted(SUPPORTED_EXTENSIONS))}"
    )


def write_transcript(df: pd.DataFrame, path: Path) -> None:
    ext = path.suffix.lower()

    if ext == ".csv":
        df.to_csv(path, index=False)
    elif ext == ".tsv":
        df.to_csv(path, sep="\t", index=False)
    elif ext in {".xlsx", ".xls"}:
        df.to_excel(path, index=False)
    else:
        raise ValueError(
            f"Unsupported output format '{ext}'. "
            f"Supported formats: {', '.join(sorted(SUPPORTED_EXTENSIONS))}"
        )


def unique_path(path: Path) -> Path:
    if not path.exists():
        return path

    counter = 2

    while True:
        candidate = path.with_name(f"{path.stem}_{counter}{path.suffix}")

        if not candidate.exists():
            return candidate

        counter += 1


def resolve_single_file_output(input_path: Path, output_arg: str | None) -> Path:
    default_filename = f"{input_path.stem}_cleaned{input_path.suffix.lower()}"

    if output_arg is None:
        output_path = input_path.parent / default_filename
    else:
        requested = Path(output_arg).expanduser()

        if requested.exists() and requested.is_dir():
            output_path = requested / default_filename
        elif requested.suffix.lower() in SUPPORTED_EXTENSIONS:
            output_path = requested
        else:
            requested.mkdir(parents=True, exist_ok=True)
            output_path = requested / default_filename

    output_path = output_path.expanduser().resolve()
    input_resolved = input_path.expanduser().resolve()

    if output_path == input_resolved:
        raise ValueError(
            "The output path resolves to the original input file. "
            "This script will not overwrite the original transcript."
        )

    output_path.parent.mkdir(parents=True, exist_ok=True)

    return unique_path(output_path)


def resolve_batch_output_directory(input_dir: Path, output_arg: str | None) -> Path:
    if output_arg is None:
        output_dir = input_dir / DEFAULT_BATCH_OUTPUT_FOLDER
    else:
        requested = Path(output_arg).expanduser()

        if requested.suffix.lower() in SUPPORTED_EXTENSIONS:
            raise ValueError(
                "When --input is a folder, --output must be a DIRECTORY path, "
                "not a file path."
            )

        if requested.exists() and requested.is_file():
            raise ValueError(
                "When --input is a folder, --output must be a DIRECTORY path, "
                "not an existing file."
            )

        output_dir = requested

    output_dir = output_dir.resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    return output_dir


def clean_one_transcript(
    input_path: Path,
    output_path: Path,
    text_column: str,
    stretch_min_length: int,
    stretch_min_run: int,
) -> dict:
    df = read_transcript(input_path)

    if text_column not in df.columns:
        raise ValueError(
            f"Text column '{text_column}' was not found. "
            f"Available columns: {', '.join(map(str, df.columns))}"
        )

    working = df.copy()

    working[text_column] = working[text_column].apply(clean_transcription)

    working["_identical_repeat_flag"] = identical_repeat_flags(
        working[text_column],
        keep=2,
    )

    working["_ab_repeat_flag"] = ab_repeat_flags(
        working[text_column],
        keep_pairs=2,
        min_pairs_to_trigger=3,
    )

    working["_stretch_hallucination_flag"] = working[text_column].apply(
        lambda text: is_extreme_stretch_hallucination(
            text,
            min_token_length=stretch_min_length,
            min_identical_run=stretch_min_run,
        )
    )

    identical_count = int(working["_identical_repeat_flag"].sum())
    ab_count = int(working["_ab_repeat_flag"].sum())
    stretch_count = int(working["_stretch_hallucination_flag"].sum())

    remove_flag = (
        working["_identical_repeat_flag"]
        | working["_ab_repeat_flag"]
        | working["_stretch_hallucination_flag"]
    )

    total_removed = int(remove_flag.sum())

    cleaned = (
        working.loc[~remove_flag]
        .drop(
            columns=[
                "_identical_repeat_flag",
                "_ab_repeat_flag",
                "_stretch_hallucination_flag",
            ]
        )
        .reset_index(drop=True)
    )

    write_transcript(cleaned, output_path)

    return {
        "input": input_path,
        "output": output_path,
        "original_rows": len(df),
        "removed_rows": total_removed,
        "final_rows": len(cleaned),
        "identical_repeats": identical_count,
        "ab_repeats": ab_count,
        "stretch_hallucinations": stretch_count,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Clean one Whisper/VTC transcript or every supported transcript "
            "directly inside a folder, without modifying the originals."
        )
    )

    parser.add_argument(
        "--input",
        required=True,
        help=(
            "Path to a transcript file OR a folder containing transcript files "
            "(.csv, .tsv, .xlsx, or .xls)."
        ),
    )

    parser.add_argument(
        "--output",
        default=None,
        help=(
            "For file input: optional output directory OR full output filename. "
            "For folder input: optional output DIRECTORY only. "
            "If omitted in folder mode, uses a Cleaned_Transcripts subfolder."
        ),
    )

    parser.add_argument(
        "--text-column",
        default="sentence",
        help="Name of the transcript text column. Default: sentence",
    )

    parser.add_argument(
        "--stretch-min-length",
        type=int,
        default=20,
        help=(
            "Minimum single-token length for extreme stretch detection. "
            "Default: 20"
        ),
    )

    parser.add_argument(
        "--stretch-min-run",
        type=int,
        default=12,
        help=(
            "Minimum number of identical consecutive characters for extreme "
            "stretch detection. Default: 12"
        ),
    )

    return parser.parse_args()


def run_single_file(args: argparse.Namespace, input_path: Path) -> int:
    if input_path.suffix.lower() not in SUPPORTED_EXTENSIONS:
        print(
            f"ERROR: Unsupported input format '{input_path.suffix}'. "
            f"Supported formats: {', '.join(sorted(SUPPORTED_EXTENSIONS))}",
            file=sys.stderr,
        )
        return 1

    try:
        output_path = resolve_single_file_output(input_path, args.output)

        clean_one_transcript(
            input_path=input_path,
            output_path=output_path,
            text_column=args.text_column,
            stretch_min_length=args.stretch_min_length,
            stretch_min_run=args.stretch_min_run,
        )
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1

    print(
        f"Finished cleaning {input_path.name}. "
        f"Saved to {output_path}."
    )

    return 0


def run_folder(args: argparse.Namespace, input_dir: Path) -> int:
    try:
        output_dir = resolve_batch_output_directory(input_dir, args.output)
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1

    transcript_files = sorted(
        p
        for p in input_dir.iterdir()
        if p.is_file() and p.suffix.lower() in SUPPORTED_EXTENSIONS
    )

    if not transcript_files:
        print(
            f"ERROR: No supported transcript files were found directly inside: "
            f"{input_dir}",
            file=sys.stderr,
        )
        return 1

    failed = []

    for input_path in tqdm(
        transcript_files,
        desc="Cleaning transcripts",
        unit="file",
    ):
        default_name = f"{input_path.stem}_cleaned{input_path.suffix.lower()}"
        output_path = unique_path(output_dir / default_name)

        try:
            clean_one_transcript(
                input_path=input_path,
                output_path=output_path,
                text_column=args.text_column,
                stretch_min_length=args.stretch_min_length,
                stretch_min_run=args.stretch_min_run,
            )
        except Exception as exc:
            failed.append((input_path.name, str(exc)))

    if failed:
        for filename, error in failed:
            print(f"ERROR: {filename}: {error}", file=sys.stderr)
        return 1

    print(
        f"Finished cleaning all input transcripts. "
        f"Saved to {output_dir}."
    )

    return 0


def main() -> int:
    args = parse_args()

    if args.stretch_min_length < 1 or args.stretch_min_run < 2:
        print(
            "ERROR: --stretch-min-length must be >= 1 and "
            "--stretch-min-run must be >= 2.",
            file=sys.stderr,
        )
        return 1

    input_path = Path(args.input).expanduser().resolve()

    if not input_path.exists():
        print(f"ERROR: Input path does not exist: {input_path}", file=sys.stderr)
        return 1

    if input_path.is_file():
        return run_single_file(args, input_path)

    if input_path.is_dir():
        return run_folder(args, input_path)

    print(
        f"ERROR: --input must point to a transcript file or directory: {input_path}",
        file=sys.stderr,
    )
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
