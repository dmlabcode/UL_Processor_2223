import os
import argparse
import pandas as pd
from decimal import Decimal, ROUND_HALF_UP

def get_mlu(subset):
    """
    Calculates the mean length of utterance for a given subset of data.
    Rounds to the nearest hundredth using schoolbook rounding (ROUND_HALF_UP)
    and returns a string strictly formatted to two decimal places.
    """
    if len(subset) == 0:
        return "0.00"
        
    total_words = Decimal(int(subset['word_count'].sum()))
    total_rows = Decimal(len(subset))
    
    mlu = total_words / total_rows
    
    # Quantize to 0.01 using standard schoolbook rounding (half away from zero)
    rounded_mlu = mlu.quantize(Decimal('0.01'), rounding=ROUND_HALF_UP)
    
    # Format to strictly enforce 2 decimal places in the string output
    return f"{rounded_mlu:.2f}"

def calculate_mlu(input_files, output_csv):
    results = []
    
    for file in input_files:
        try:
            df = pd.read_csv(file)
        except Exception as e:
            print(f"Error reading {file}: {e}")
            continue
            
        # Handle potential empty sentences
        if 'sentence' not in df.columns:
            print(f"Skipping {file}: 'sentence' column not found.")
            continue
            
        df['sentence'] = df['sentence'].fillna("")
        
        # Count words per row (splitting by whitespace)
        df['word_count'] = df['sentence'].apply(lambda x: len(str(x).split()))
        
        # Identify if the utterance is a question (ends with '?')
        # Strip whitespace first to ensure trailing spaces don't hide the punctuation
        df['is_question'] = df['sentence'].astype(str).str.strip().str.endswith('?')
        
        file_name = os.path.basename(file)
        
        if 'speaker' not in df.columns:
            print(f"Skipping {file}: 'speaker' column not found.")
            continue
            
        unique_speakers = df['speaker'].dropna().unique()
        
        # Auto-detect label type by checking for any of the alternate labels
        alt_labels_set = {'Teacher', 'Male_Adult', 'Other_Child', 'Main_Child'}
        is_alt = any(s in alt_labels_set for s in unique_speakers)
        
        if is_alt:
            base_labels = ['Teacher', 'Male_Adult', 'Other_Child', 'Main_Child', 'Unidentified_Speaker']
            adult_labels = ['Teacher', 'Male_Adult']
            child_labels = ['Other_Child', 'Main_Child']
        else:
            base_labels = ['FEM', 'MAL', 'CHI', 'KCHI', 'Unidentified_Speaker']
            adult_labels = ['FEM', 'MAL']
            child_labels = ['CHI', 'KCHI']
            
        # 1. Calculate for the 5 base labels
        for label in base_labels:
            subset = df[df['speaker'] == label]
            results.append({
                'file_name': file_name,
                'speaker': label,
                'mlu': get_mlu(subset),
                'question_mlu': get_mlu(subset[subset['is_question']]),
                'non_question_mlu': get_mlu(subset[~subset['is_question']])
            })
            
        # 2. Calculate for the 'adult' composite category
        adult_subset = df[df['speaker'].isin(adult_labels)]
        results.append({
            'file_name': file_name,
            'speaker': 'Adult',
            'mlu': get_mlu(adult_subset),
            'question_mlu': get_mlu(adult_subset[adult_subset['is_question']]),
            'non_question_mlu': get_mlu(adult_subset[~adult_subset['is_question']])
        })
        
        # 3. Calculate for the 'child' composite category
        child_subset = df[df['speaker'].isin(child_labels)]
        results.append({
            'file_name': file_name,
            'speaker': 'Child',
            'mlu': get_mlu(child_subset),
            'question_mlu': get_mlu(child_subset[child_subset['is_question']]),
            'non_question_mlu': get_mlu(child_subset[~child_subset['is_question']])
        })
        
    # Create the final dataframe and save to CSV
    if results:
        results_df = pd.DataFrame(results)
        results_df = results_df[['file_name', 'speaker', 'mlu', 'question_mlu', 'non_question_mlu']]
        results_df.to_csv(output_csv, index=False)
        print(f"Successfully saved MLU calculations to: {output_csv}")
    else:
        print("No data was processed.")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Calculate MLU from processed ALICE CSV files.")
    parser.add_argument("input_path", help="Path to a single processed CSV file or a directory containing CSV files.")
    parser.add_argument("--output", default="mlu_summary.csv", help="Filename for the output CSV.")
    
    args = parser.parse_args()
    
    input_path = args.input_path
    files_to_process = []
    
    # Check if the path is a file or a directory
    if os.path.isfile(input_path):
        if input_path.endswith('.csv'):
            files_to_process.append(input_path)
        else:
            print(f"Error: {input_path} is not a .csv file.")
    elif os.path.isdir(input_path):
        for f in os.listdir(input_path):
            if f.endswith('.csv'):
                files_to_process.append(os.path.join(input_path, f))
    else:
        print(f"Error: The path '{input_path}' is neither a valid file nor a directory.")
        exit(1)
        
    if not files_to_process:
        print(f"No CSV files found to process in '{input_path}'.")
    else:
        print(f"Found {len(files_to_process)} CSV file(s) to process.")
        calculate_mlu(files_to_process, args.output)