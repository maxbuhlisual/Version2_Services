"""HTTP regression check. Run: python3 scripts/check_expense_security.py
Requires .NET 10 SDK, Python 3 and OpenSSL. All generated files are temporary.
"""
import pathlib, tempfile, subprocess, os, ssl, urllib.request, urllib.error, http.cookiejar, json, time, sqlite3
import socket
import sys

if not __debug__:
    raise RuntimeError("Run without -O: assertions are required for this check")
root=pathlib.Path(__file__).resolve().parents[1]
subprocess.run(['dotnet', 'build', str(root/'ExpenseTracker.sln'), '--disable-build-servers'],
               cwd=root, check=True, timeout=180)
with tempfile.TemporaryDirectory(prefix='expense-cookie-') as temp:
    temp=pathlib.Path(temp)
    subprocess.run(['openssl','req','-x509','-newkey','rsa:2048','-nodes','-keyout',str(temp/'key.pem'),'-out',str(temp/'cert.pem'),'-days','1','-subj','/CN=localhost'],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
    subprocess.run(['openssl','pkcs12','-export','-out',str(temp/'test.pfx'),'-inkey',str(temp/'key.pem'),'-in',str(temp/'cert.pem'),'-passout','pass:temporary-test'],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
    db=temp/'test.db'
    env=dict(os.environ,ConnectionStrings__ExpenseTracker=f'Data Source={db};Pooling=False',ASPNETCORE_ENVIRONMENT='Development',ASPNETCORE_Kestrel__Certificates__Default__Path=str(temp/'test.pfx'),ASPNETCORE_Kestrel__Certificates__Default__Password='temporary-test')
    with socket.socket() as listener:
        listener.bind(('127.0.0.1', 0))
        port = listener.getsockname()[1]
    base=f'https://localhost:{port}'
    def client():
        jar=http.cookiejar.CookieJar()
        opener=urllib.request.build_opener(urllib.request.HTTPSHandler(context=ssl.create_default_context(cafile=str(temp/'cert.pem'))),urllib.request.HTTPCookieProcessor(jar))
        return opener,jar
    def call(client,path,data=None,token=None):
        headers={}
        if data is not None: headers['Content-Type']='application/json'
        if token is not None: headers['X-CSRF-TOKEN']=token
        req=urllib.request.Request(base+path,data=None if data is None else json.dumps(data).encode(),headers=headers)
        try:
            with client[0].open(req,timeout=5) as response:
                raw=response.read().decode()
                return response.status,dict(response.headers),json.loads(raw) if raw else None
        except urllib.error.HTTPError as error:
            return error.code,dict(error.headers),error.read().decode()
    def token(client):
        status,_,body=call(client,'/api/Users/csrf')
        assert status==200,(status,body)
        return body['token']
    def post(client,path,data):
        return call(client,path,data,token(client))
    with open(temp/'api.log','w+') as log:
        process=subprocess.Popen(['dotnet',str(root/'ExpenseTracker.Api/bin/Debug/net10.0/ExpenseTracker.Api.dll'),'--urls',base],cwd=root/'ExpenseTracker.Api',env=env,stdout=log,stderr=subprocess.STDOUT)
        try:
            a,b,guest=client(),client(),client()
            for _ in range(150):
                if process.poll() is not None:
                    log.seek(0);raise RuntimeError(log.read())
                try:
                    if call(guest,'/api/Users/csrf')[0]==200:break
                except urllib.error.URLError:time.sleep(.1)
            else:
                log.flush();log.seek(0);print(log.read());print(call(guest,'/api/Users/csrf'));raise RuntimeError('startup timeout')
            users=[]
            for label,c in [('A',a),('B',b)]:
                status,_,user=post(c,'/api/Users/register',{'email':label+'@example.invalid','name':label,'password':'Fictional123!'})
                assert status==200,(status,user)
                status,headers,body=post(c,'/api/Users/login',{'email':label+'@example.invalid','password':'Fictional123!'})
                assert status==200 and body==user,(status,body)
                cookie=headers.get('Set-Cookie','')
                assert all(flag in cookie.lower() for flag in ['httponly','secure','samesite=strict']),cookie
                assert any(x.name=='__Host-ExpenseTracker-Session' and x.secure for x in c[1])
                users.append(user)
                print(label,'login',status,'JSON unchanged; cookie Secure, HttpOnly, SameSite=Strict')
            ua,ub=users
            categories=[]
            for c,u in [(a,ua),(b,ub)]:
                status,_,category=post(c,'/api/categories',{'userId':u['id'],'name':'Category '+u['name'],'parentId':None})
                assert status==200,(status,category)
                categories.append(category)
            ca,cb=categories
            status,_,body=post(b,'/api/budgets',{'userId':ub['id'],'categoryId':cb['id'],'year':2026,'month':11,'limit':2000})
            assert status==200,(status,body)
            expense={'userId':ub['id'],'categoryId':cb['id'],'amount':300,'date':'2026-11-03T12:00:00Z','comment':'Cookie security B'}
            status,_,created=post(b,'/api/expenses',expense)
            assert status==200,(status,created)
            print('B create',status)
            def snapshot():
                with sqlite3.connect(db) as connection:
                    return connection.execute('SELECT COUNT(*) FROM Expenses').fetchone()[0],connection.execute('SELECT Spent FROM Budgets').fetchone()[0]
            baseline=snapshot()
            assert baseline == (1, "300"), baseline
            results=[]
            for label,c,path,data,csrf,expected in [
                ('B reads own',b,'/api/expenses/'+ub['id'],None,None,200),
                ('guest reads B',guest,'/api/expenses/'+ub['id'],None,None,401),
                ('guest writes no csrf',guest,'/api/expenses',expense,None,401),
                ('guest writes with csrf',guest,'/api/expenses',expense,token(guest),401),
                ('A reads B',a,'/api/expenses/'+ub['id'],None,None,403),
                ('A writes as B',a,'/api/expenses',expense,token(a),403),
                ('A uses category B',a,'/api/expenses',dict(expense,userId=ua['id']),token(a),403),
                ('B writes without csrf',b,'/api/expenses',expense,None,400),
                ('B writes invalid csrf',b,'/api/expenses',expense,'invalid-token',400),
                ('B uses A csrf',b,'/api/expenses',expense,token(a),400)]:
                status,_,body=call(c,path,data,csrf)
                assert status==expected,(label,status,body)
                if label=='B reads own':assert body==[created]
                assert snapshot()==baseline,(label,snapshot(),baseline)
                results.append({'case':label,'status':status,'database':snapshot()})
            print(json.dumps(results,indent=2))
            # Swagger interceptor is present in generated UI configuration.
            with b[0].open(base+'/swagger/index.html') as response:
                html=response.read().decode()
            for script in ['index.js', 'swagger-ui-init.js']:
                try:
                    with b[0].open(base+'/swagger/'+script) as response: js=response.read().decode()
                    if 'X-CSRF-TOKEN' in js:
                        print('CSRF interceptor found in',script);break
                except urllib.error.HTTPError: pass
            else: raise AssertionError('CSRF interceptor not found')
            print('PASS: all HTTP checks; expense count and budget Spent:',snapshot())
        finally:
            process.terminate()
            try:process.wait(timeout=10)
            except subprocess.TimeoutExpired:process.kill();process.wait()
