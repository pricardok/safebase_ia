import json
import urllib.request

url = 'http://127.0.0.1:8000/ingest/agent-data'
data = {
    'agent_id': 'agent-001',
    'timestamp': '2026-04-23T12:00:00Z',
    'payload_type': 'test_payload',
    'payload_data': {'message': 'Teste de envio com API Key', 'value': 123},
    'metadata': {'source': 'python-test'}
}

body = json.dumps(data).encode('utf-8')
req = urllib.request.Request(url, data=body, method='POST')
req.add_header('Content-Type', 'application/json')
req.add_header('X-API-Key', 'Hx7z9Q2wR4mP6tY8uVbN3cL1sZ0kF5hE')

with urllib.request.urlopen(req, timeout=10) as resp:
    print(resp.status)
    print(resp.read().decode('utf-8'))
