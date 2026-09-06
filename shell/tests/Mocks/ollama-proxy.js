const http = require('http');

const PORT = 11435;
const OLLAMA_PORT = 11434;

const server = http.createServer((req, res) => {
    console.log(`[Proxy] Request: ${req.method} ${req.url}`);
    
    // Forward request to Ollama
    const options = {
        hostname: '127.0.0.1',
        port: OLLAMA_PORT,
        path: req.url,
        method: req.method,
        headers: req.headers
    };
    
    // Delete host header to avoid conflicts
    delete options.headers['host'];
    
    const proxyReq = http.request(options, (proxyRes) => {
        console.log(`[Proxy] Response status from Ollama: ${proxyRes.statusCode} for ${req.url}`);
        
        // Parse the pathname to ignore query parameters
        const parsedUrl = new URL(req.url, 'http://127.0.0.1');
        const pathname = parsedUrl.pathname;
        
        // If the path is /v1/models, rewrite the response to include "models" field for Codex
        if ((pathname === '/v1/models' || pathname === '/v1/models/') && proxyRes.statusCode === 200) {
            let body = [];
            proxyRes.on('data', (chunk) => body.push(chunk));
            proxyRes.on('end', () => {
                const data = Buffer.concat(body);
                try {
                    const json = JSON.parse(data.toString());
                    if (json.data && !json.models) {
                        // Inject all required catalog fields including experimental_supported_tools to satisfy Codex CLI's deserializer
                        json.models = json.data.map(item => {
                            return {
                                ...item,
                                slug: item.id,
                                display_name: item.id,
                                description: `Ollama model: ${item.id}`,
                                context_window: 8192,
                                max_context_window: 8192,
                                default_reasoning_level: "none",
                                supported_reasoning_levels: [],
                                supports_reasoning_summaries: false,
                                shell_type: "shell_command",
                                visibility: "list",
                                supported_in_api: true,
                                supports_vision: false,
                                supports_function_calling: false,
                                priority: 0,
                                deprecated: false,
                                tier: "free",
                                supports_tools: false,
                                is_reasoning_model: false,
                                base_instructions: "",
                                support_verbosity: false,
                                supports_verbosity: false,
                                truncation_policy: {
                                    mode: "tokens",
                                    limit: 8192
                                },
                                supports_parallel_tool_calls: false,
                                supports_image_input: false,
                                supports_audio_input: false,
                                supports_video_input: false,
                                supports_document_input: false,
                                experimental_supported_tools: [],
                                is_default: false,
                                is_deprecated: false
                            };
                        });
                        const modifiedData = JSON.stringify(json);
                        console.log(`[Proxy] Rewriting /v1/models response to inject comprehensive 'models' schema fields including experimental_supported_tools`);
                        res.writeHead(200, {
                            'Content-Type': 'application/json',
                            'Content-Length': Buffer.byteLength(modifiedData)
                        });
                        res.end(modifiedData);
                        return;
                    }
                } catch (e) {
                    console.error(`[Proxy] Error parsing /v1/models response: ${e.message}`);
                }
                res.writeHead(proxyRes.statusCode, proxyRes.headers);
                res.end(data);
            });
        } else {
            // For all other routes (like streaming responses), pipe directly to preserve streaming/SSE
            res.writeHead(proxyRes.statusCode, proxyRes.headers);
            proxyRes.pipe(res);
        }
    });
    
    proxyReq.on('error', (err) => {
        console.error(`[Proxy] Error forwarding request: ${err.message}`);
        try {
            res.writeHead(500);
            res.end(err.message);
        } catch (e) {}
    });
    
    req.on('error', (err) => {
        console.error(`[Proxy] Client request error: ${err.message}`);
    });
    
    res.on('error', (err) => {
        console.error(`[Proxy] Client response error: ${err.message}`);
    });
    
    req.pipe(proxyReq);
});

server.listen(PORT, '127.0.0.1', () => {
    console.log(`Ollama Proxy listening on port ${PORT}`);
});
