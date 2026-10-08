"""Use the Editor's existing framed loopback MCP transport; file arguments avoid shell quoting."""
import socket, struct, json, sys
from pathlib import Path
def call(name, args=None):
    with socket.create_connection(('127.0.0.1',38000), timeout=45) as s:
        s.recv(1024) # FRAMING=1 handshake
        payload=json.dumps({'type':name,'params':args or {}}).encode('utf-8')
        s.sendall(struct.pack('>Q',len(payload))+payload)
        def read(n):
            out=b''
            while len(out)<n:
                chunk=s.recv(n-len(out))
                if not chunk: raise ConnectionError('Editor closed connection')
                out+=chunk
            return out
        return json.loads(read(struct.unpack('>Q',read(8))[0]))
if __name__=='__main__':
    name=sys.argv[1]
    if name=='code':
        result=call('execute_code',{'action':'execute','code':Path(sys.argv[2]).read_text(encoding='utf-8-sig')})
    else: result=call(name,json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig')) if len(sys.argv)>2 else {})
    print(json.dumps(result,ensure_ascii=True))
