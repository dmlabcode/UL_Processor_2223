import os
import re
import argparse
import pandas as pd

def parse_rttm(rttm_path):
    """Parses the ALICE RTTM file"""
    with open(rttm_path, 'r') as file:
        rttm_contents = file.readlines()

    rttm_data = []
    for line in rttm_contents:
        parts = line.split()
        if len(parts) < 8:
            continue
            
        speaker_class = parts[7]
        if speaker_class in ['CHI', 'FEM', 'KCHI', 'MAL']:
            file_name = parts[1]
            start_time = float(parts[3])
            duration = float(parts[4])
            end_time = start_time + duration
            rttm_data.append([file_name, start_time, end_time, speaker_class, duration])

    return pd.DataFrame(rttm_data, columns=['file_name', 'start_time', 'end_time', 'speaker_class', 'duration'])

def match_rttm_filename(target_filename, rttm_filenames):
    """Token-based matching to align CSV filenames with RTTM filenames."""
    
    # 1. Hardcoded exception for the known duplicate
    if "Starfish_32" in target_filename and "066" in target_filename:
        if "_45_" in target_filename:
            return "Starfish_32_066_01_04172023_clip_030650to035150"
        elif "_10_" in target_filename:
            return "Starfish_32_066_01_04172023_clip_022400to023400"
            
    # 2. Extract the core ID string between WhisperCoding_ and _01/_02
    match = re.search(r'WhisperCoding2?_(.*?)_(?:01|02)', target_filename, re.IGNORECASE)
    if not match:
        return None
        
    core_id_string = match.group(1)
    
    # 3. Tokenize and clean
    excel_tokens = [t.lower() for t in core_id_string.split('_') if t.lower() != 'child']
    
    # 4. Search through RTTM unique filenames
    for rname in rttm_filenames:
        rttm_tokens = [t.lower() for t in re.split(r'[-_]', rname)]
        if all(token in rttm_tokens for token in excel_tokens):
            return rname
            
    return None

def get_dominant_speaker(start, end, df_rttm_filtered):
    """Calculates overlaps and returns the ALICE speaker_class with max time."""
    if pd.isna(start) or pd.isna(end) or df_rttm_filtered.empty:
        return None
        
    mask = (df_rttm_filtered['start_time'] < end) & (df_rttm_filtered['end_time'] > start)
    overlaps = df_rttm_filtered.loc[mask].copy()
    
    if overlaps.empty:
        return "Unidentified_Speaker"
        
    overlaps['overlap_start'] = overlaps['start_time'].clip(lower=start)
    overlaps['overlap_end'] = overlaps['end_time'].clip(upper=end)
    overlaps['overlap_duration'] = overlaps['overlap_end'] - overlaps['overlap_start']
    
    cumulative_times = overlaps.groupby('speaker_class')['overlap_duration'].sum()
    
    if cumulative_times.empty or cumulative_times.max() == 0:
        return "Unidentified_Speaker"
        
    return cumulative_times.idxmax()

def filter_whisper_loops(df):
    """
    Drops sentences that are repeated 3 or more times consecutively.
    Also drops alternating pairs of sentences that repeat 3 or more times.
    """
    valid_indices = []
    # Convert sentences to a list of strings for safe, index-based lookbacks
    sentences = df['sentence'].fillna("").astype(str).str.strip().tolist()

    for i in range(len(sentences)):
        curr = sentences[i]
        
        # Keep empty strings to avoid breaking timeline formatting
        if curr == "":
            valid_indices.append(df.index[i])
            continue
            
        drop = False
        
        # 1. Unigram Loop Check: A, A, [A]
        if i >= 2 and curr == sentences[i-1] == sentences[i-2]:
            drop = True
            
        # 2. Bigram Loop Check (1st element of the pair): A, B, A, B, [A]
        elif i >= 4 and curr == sentences[i-2] == sentences[i-4] and sentences[i-1] == sentences[i-3]:
            drop = True
            
        # 3. Bigram Loop Check (2nd element of the pair): A, B, A, B, A, [B]
        elif i >= 5 and curr == sentences[i-2] == sentences[i-4] and sentences[i-1] == sentences[i-3] == sentences[i-5]:
            drop = True
            
        if not drop:
            valid_indices.append(df.index[i])

    # Return a new dataframe with only the valid rows
    return df.loc[valid_indices].copy()

def align_alice_labels(csv_path, rttm_path, use_alt_labels=False):
    print(f"Loading ALICE RTTM file: {rttm_path}")
    rttm_df = parse_rttm(rttm_path)
    rttm_unique_files = rttm_df['file_name'].unique()
    
    csv_filename = os.path.basename(csv_path)
    
    # If the RTTM file only has one unique recording, default to it. 
    # Otherwise, use the regex matcher.
    if len(rttm_unique_files) == 1:
        matched_rttm_name = rttm_unique_files[0]
    else:
        matched_rttm_name = match_rttm_filename(csv_filename, rttm_unique_files)
        
    if not matched_rttm_name:
        print(f"Error: Could not find a matching ID in the RTTM file for {csv_filename}.")
        return
        
    file_rttm_df = rttm_df[rttm_df['file_name'] == matched_rttm_name].copy()
    
    try:
        df_csv = pd.read_csv(csv_path)
    except Exception as e:
        print(f"Error reading {csv_path}: {e}")
        return
        
    # 1. Filter out Whisper loops (Both Unigram and Bigram)
    df_filtered = filter_whisper_loops(df_csv)
    
    # Mapping for alternate labels
    alt_label_map = {
        'FEM': 'Teacher',
        'MAL': 'Male_Adult',
        'CHI': 'Other_Child',
        'KCHI': 'Main_Child'
    }
    
    # 2. Match overlapping times to get speakers
    new_alice_labels = []
    
    for idx, row in df_filtered.iterrows():
        start = row.get('start_sec')
        end = row.get('end_sec')
        
        dominant_speaker = get_dominant_speaker(start, end, file_rttm_df)
        
        if use_alt_labels and dominant_speaker in alt_label_map:
            dominant_speaker = alt_label_map[dominant_speaker]
            
        new_alice_labels.append(dominant_speaker)
        
    df_filtered['speaker'] = new_alice_labels
    
    # 3. Format the final output: start_sec, end_sec, timestamp, sentence, speaker
    final_df = df_filtered[['start_sec', 'end_sec', 'time_start', 'sentence', 'speaker']]
    
    base_name, ext = os.path.splitext(csv_path)
    out_name = f"{base_name}_ALICE.csv"
    
    final_df.to_csv(out_name, index=False)
    print(f"Processed: {csv_filename} -> matched with RTTM ID: {matched_rttm_name}")
    print(f"Output saved to: {out_name}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Add ALICE speaker labels to a Whisper CSV file.")
    parser.add_argument("csv_file", help="Path to the Whisper .csv file.")
    parser.add_argument("rttm_file", help="Path to the ALICE diarization_output.rttm file.")
    parser.add_argument("--alt-labels", action="store_true", help="Use alternate speaker labels (Teacher, Male_Adult, Other_Child, Main_Child)")
    
    args = parser.parse_args()
    align_alice_labels(args.csv_file, args.rttm_file, args.alt_labels)

