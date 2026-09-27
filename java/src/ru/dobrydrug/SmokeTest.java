package ru.dobrydrug;

import com.sun.net.httpserver.*;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.time.*;
import java.util.*;

/** Run with -ea. Exercises the main workflows without opening a network port. */
public class SmokeTest {
    public static void main(String[] args) throws Exception {
        Main.db = new Main.Store();
        Main.User manager = user("manager@example.test", "Manager");
        Main.User consultant = user("consultant@example.test", "Consultant");
        Main.User visitor = user("visitor@example.test", "Visitor");
        Mock managerSession = login(manager), consultantSession = login(consultant), visitorSession = login(visitor);

        Mock create = request("POST", "/api/animals", "name=Бим&type=Собака&gender=Мужской&age=2&breed=Метис&color=Рыжий", managerSession);
        Main.api(create);
        assert create.responseCode == 200;
        assert create.output.toString(StandardCharsets.UTF_8).contains("Бим");
        long animalId = Main.db.animals.keySet().iterator().next();

        Mock vaccine = request("POST", "/api/animals/"+animalId+"/vaccinations", "name=Бешенство&date=2026-01-01", managerSession);
        Main.api(vaccine);
        assert Main.db.animals.get(animalId).vaccinations.size() == 1;
        Mock medical = request("POST", "/api/animals/"+animalId+"/medical", "date=2026-01-02&note=Осмотр", managerSession);
        Main.api(medical);
        assert Main.db.animals.get(animalId).medical.size() == 1;

        Mock apply = request("POST", "/api/applications", "animalId="+animalId+"&name=Иван+Иванов&contact=123&address=Москва", visitorSession);
        Main.api(apply);
        long applicationId = Main.db.applications.keySet().iterator().next();
        assert !Main.db.animals.get(animalId).adopted;

        status(applicationId, "APPROVED", consultantSession);
        status(applicationId, "COMPLETED", consultantSession);
        assert Main.db.animals.get(animalId).adopted;
        assert Main.db.applications.get(applicationId).history.size() == 3;
        Mock review = request("POST", "/api/reviews", "animalId=0&content=Спасибо+приюту", visitorSession);
        Main.api(review);
        assert Main.db.reviews.size() == 1;
        assert !Main.db.reviews.values().iterator().next().approved;
        try { Main.api(request("POST", "/api/applications", "animalId="+animalId+"&name=Другой&contact=123&address=Москва", visitorSession)); throw new AssertionError("Adopted animal accepted an application"); }
        catch (Main.Problem e) { assert e.code == 409; }

        Mock report = request("GET", "/api/reports?from=2026-01-01&to=2030-01-01", "", managerSession);
        Main.api(report);
        assert report.output.toString(StandardCharsets.UTF_8).contains("\"adopted\":1");

        try { Main.api(request("POST", "/api/animals", "name=X&type=Кот&gender=Женский&age=1", visitorSession)); throw new AssertionError("Visitor created an animal"); }
        catch (Main.Problem e) { assert e.code == 403; }
        Main.Store demo = new Main.Store();
        SeedData.apply(demo);
        assert demo.animals.size() == 6 && demo.applications.size() == 3 && demo.reviews.size() == 4;
        SeedData.apply(demo);
        assert demo.animals.size() == 6;
        System.out.println("Smoke tests passed: cards, medical records, applications, reviews, roles, reports, demo data.");
    }
    static Main.User user(String email,String role) {
        Main.User u = new Main.User(); u.id=Main.db.nextId++;u.name=email;u.email=email;u.role=role;
        Main.setPassword(u,"Password123!");
        assert Main.verify(u,"Password123!");
        assert !Main.verify(u,"wrong");
        Main.db.users.put(u.id,u);return u;
    }
    static Mock login(Main.User u) {Mock m=new Mock("GET","/api/me","");Main.login(m,u);String set=m.responseHeaders.getFirst("Set-Cookie");m.cookie=set.substring(0,set.indexOf(';'));m.csrf=Main.sessions.get(m.cookie.substring(4)).csrf();return m;}
    static Mock request(String method,String path,String body,Mock session) {Mock m=new Mock(method,path,body);m.cookie=session.cookie;m.csrf=session.csrf;return m;}
    static void status(long id,String status,Mock session)throws Exception {Mock x=request("POST","/api/applications/"+id+"/status","status="+status,session);Main.api(x);assert x.responseCode==200;}
    static class Mock extends HttpExchange {
        final Headers requestHeaders=new Headers(),responseHeaders=new Headers();
        final URI uri;final String method;InputStream input;ByteArrayOutputStream output=new ByteArrayOutputStream();int responseCode;
        String cookie,csrf;
        Mock(String method,String path,String body){this.method=method;uri=URI.create(path.replace(" ","%20"));input=new ByteArrayInputStream(body.getBytes(StandardCharsets.UTF_8));}
        public Headers getRequestHeaders(){if(cookie!=null)requestHeaders.set("Cookie",cookie);if(csrf!=null)requestHeaders.set("X-CSRF-Token",csrf);return requestHeaders;}
        public Headers getResponseHeaders(){return responseHeaders;}
        public URI getRequestURI(){return uri;}
        public String getRequestMethod(){return method;}
        public HttpContext getHttpContext(){return null;}
        public void close(){}
        public InputStream getRequestBody(){return input;}
        public OutputStream getResponseBody(){return output;}
        public void sendResponseHeaders(int code,long length){responseCode=code;}
        public InetSocketAddress getRemoteAddress(){return new InetSocketAddress("127.0.0.1",12345);}
        public int getResponseCode(){return responseCode;}
        public InetSocketAddress getLocalAddress(){return new InetSocketAddress("127.0.0.1",8080);}
        public String getProtocol(){return "HTTP/1.1";}
        public Object getAttribute(String key){return null;}
        public void setAttribute(String key,Object value){}
        public void setStreams(InputStream input,OutputStream output){this.input=input;}
        public HttpPrincipal getPrincipal(){return null;}
    }
}
