import sys
import os
import json
from bm25_core import BM25, load_index

def search(query, top_n=5):
    PROJECT_ROOT = "c:\\Users\\TruongNhon\\Documents\\Powershell\\antigrafity-config\\mission-control-master"
    INDEX_CSV = os.path.join(PROJECT_ROOT, "data", "bm25_index.csv")
    
    if not os.path.exists(INDEX_CSV):
        return {"error": "Index not found. Please run scripts/bm25_indexer.py first."}
    
    data = load_index(INDEX_CSV)
    corpus = [item['content'] for item in data]
    
    bm25 = BM25()
    bm25.fit(corpus)
    scores = bm25.get_scores(query)
    
    # Sort and take top N
    results = []
    indices = sorted(range(len(scores)), key=lambda i: scores[i], reverse=True)[:top_n]
    
    for idx in indices:
        if scores[idx] > 0:
            results.append({
                "score": round(scores[idx], 2),
                "file": data[idx]['file_path'],
                "header": data[idx]['type'],
                "content": data[idx]['content'][:500] + "..." if len(data[idx]['content']) > 500 else data[idx]['content']
            })
            
    return results

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python bm25_search.py <query>")
        sys.exit(1)
        
    query = " ".join(sys.argv[1:])
    results = search(query)
    
    if isinstance(results, dict) and "error" in results:
        print(results["error"])
    else:
        print(f"\n--- BM25 Search Results for: '{query}' ---")
        if not results:
            print("No relevant configuration chunks found.")
        for i, res in enumerate(results, 1):
            print(f"\n[{i}] Score: {res['score']} | File: {res['file']}")
            print(f"    Target: {res['header']}")
            print(f"    Snippet: {res['content']}")
        print("\n--- End of Results ---")
