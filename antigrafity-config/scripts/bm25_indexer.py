import os
import re
from bm25_core import save_index

def index_project(root_dir):
    index_data = []
    chunk_id = 1
    
    # Folders to scan relative to root
    scan_folders = ['agent/rules', 'agent/rules/project', 'agent/rules/archive', 'agent/workflows', 'agent/skills', 'agent/reference', 'docs', 'documentation', '']
    
    for folder in scan_folders:
        folder_path = os.path.join(root_dir, folder)
        if not os.path.exists(folder_path):
            continue
            
        if folder == '':
            # Just root level files
            items = os.listdir(folder_path)
            for item in items:
                if item.lower().endswith('.md') and os.path.isfile(os.path.join(folder_path, item)):
                    chunk_id = process_file(os.path.join(folder_path, item), root_dir, index_data, chunk_id)
            continue

        for root, dirs, files in os.walk(folder_path):
            # Skip common noise
            if '__pycache__' in root or 'node_modules' in root or '.git' in root:
                continue
            
            for file in files:
                if file.lower().endswith('.md'):
                    chunk_id = process_file(os.path.join(root, file), root_dir, index_data, chunk_id)
                            
    return index_data

def process_file(file_path, root_dir, index_data, chunk_id):
    rel_path = os.path.relpath(file_path, root_dir)
    print(f"Processing: {rel_path}")
    
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
            
            # Split by headers (e.g., #, ##, ###)
            chunks = re.split(r'^(#+ .*)$', content, flags=re.MULTILINE)
            
            current_header = os.path.basename(file_path)
            for chunk in chunks:
                if not chunk.strip():
                    continue
                
                if chunk.startswith('#'):
                    current_header = chunk.strip()
                    continue
                
                # Add chunk to index
                index_data.append({
                    'id': chunk_id,
                    'file_path': rel_path,
                    'type': current_header,
                    'content': chunk.strip()
                })
                chunk_id += 1
    except Exception as e:
        print(f"Error processing {rel_path}: {e}")
        
    return chunk_id

if __name__ == "__main__":
    PROJECT_ROOT = "c:\\Users\\TruongNhon\\Documents\\Powershell\\antigrafity-config\\mission-control-master"
    DATA_DIR = os.path.join(PROJECT_ROOT, "data")
    INDEX_CSV = os.path.join(DATA_DIR, "bm25_index.csv")
    
    print(f"Indexing files in {PROJECT_ROOT}...")
    data = index_project(PROJECT_ROOT)
    save_index(INDEX_CSV, data)
    print(f"Successfully indexed {len(data)} chunks into {INDEX_CSV}")
