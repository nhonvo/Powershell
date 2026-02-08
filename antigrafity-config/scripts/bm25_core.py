import math
import re
import csv
from collections import Counter

class BM25:
    def __init__(self, k1=1.5, b=0.75):
        self.k1 = k1
        self.b = b
        self.corpus_size = 0
        self.avgdl = 0
        self.doc_freqs = []
        self.idf = {}
        self.doc_lengths = []

    def tokenize(self, text):
        return re.findall(r'\b\w+\b', text.lower())

    def fit(self, corpus):
        """
        corpus: List of strings (documents)
        """
        self.corpus_size = len(corpus)
        tokenized_corpus = [self.tokenize(doc) for doc in corpus]
        self.doc_lengths = [len(doc) for doc in tokenized_corpus]
        self.avgdl = sum(self.doc_lengths) / self.corpus_size if self.corpus_size > 0 else 0
        
        nd = {}  # n(qi): number of documents that contain term qi
        for doc in tokenized_corpus:
            self.doc_freqs.append(Counter(doc))
            for word in set(doc):
                nd[word] = nd.get(word, 0) + 1
        
        for word, n_qi in nd.items():
            # idf(qi) = log((N - n(qi) + 0.5) / (n(qi) + 0.5) + 1)
            self.idf[word] = math.log((self.corpus_size - n_qi + 0.5) / (n_qi + 0.5) + 1)

    def get_scores(self, query):
        query_tokens = self.tokenize(query)
        scores = []
        for i in range(self.corpus_size):
            score = 0
            doc_freq = self.doc_freqs[i]
            d_len = self.doc_lengths[i]
            for token in query_tokens:
                if token not in self.idf:
                    continue
                f_qi_d = doc_freq.get(token, 0)
                idf = self.idf[token]
                # score(D, Q) = sum( IDF(qi) * (f(qi, D) * (k1 + 1)) / (f(qi, D) + k1 * (1 - b + b * (|D| / avgdl))) )
                numerator = f_qi_d * (self.k1 + 1)
                denominator = f_qi_d + self.k1 * (1 - self.b + self.b * (d_len / self.avgdl))
                score += idf * (numerator / denominator)
            scores.append(score)
        return scores

def save_index(csv_path, data):
    """
    data: List of dicts with keys ['id', 'file_path', 'type', 'content']
    """
    with open(csv_path, 'w', encoding='utf-8', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=['id', 'file_path', 'type', 'content'])
        writer.writeheader()
        writer.writerows(data)

def load_index(csv_path):
    data = []
    with open(csv_path, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f)
        for row in reader:
            data.append(row)
    return data
