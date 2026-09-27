package ru.dobrydrug;

import com.sun.net.httpserver.*;
import javax.crypto.SecretKeyFactory;
import javax.crypto.spec.PBEKeySpec;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.*;
import java.time.*;
import java.util.*;
import java.util.concurrent.*;
import java.util.stream.Collectors;

/** Standalone Java server for Добрый друг. No external runtime dependencies. */
public class Main {
    static final Path DATA = Path.of(System.getenv().getOrDefault("SHELTER_DATA", "data/shelter.bin"));
    static final Path WEB = Path.of("java/web").toAbsolutePath().normalize();
    static Store db;
    static final Map<String, Session> sessions = new HashMap<>();
    static final SecureRandom random = new SecureRandom();
    static final Set<String> ROLES = Set.of("Admin", "Manager", "Consultant", "Controller", "Visitor");

    public static void main(String[] args) throws Exception {
        db = load();
        if (db.users.isEmpty()) {
            String password = System.getenv("SHELTER_ADMIN_PASSWORD");
            if (password == null || password.length() < 12) {
                password = token().substring(0, 18);
                System.out.println("Первый запуск. Пароль администратора: " + password);
            }
            User admin = new User(); admin.id = db.nextId++; admin.name = "Администратор";
            admin.email = "admin@animalshelter.local"; admin.role = "Admin"; setPassword(admin, password);
            db.users.put(admin.id, admin); save();
        }
        if (!db.demoSeeded && db.animals.isEmpty()) {
            SeedData.apply(db);
            save();
        }
        int port = Integer.parseInt(System.getenv().getOrDefault("PORT", "8080"));
        ExecutorService workers = Executors.newCachedThreadPool();
        try (ServerSocket server = new ServerSocket(port, 50, InetAddress.getByName("127.0.0.1"))) {
            System.out.println("Добрый друг: http://127.0.0.1:" + port);
            while (true) {
                Socket client = server.accept();
                workers.execute(() -> {
                    try { handle(new SimpleExchange(client)); }
                    catch (Exception e) { try { client.close(); } catch (IOException ignored) {} }
                });
            }
        } finally { workers.shutdownNow(); }
    }

    static synchronized Store load() throws Exception {
        if (!Files.exists(DATA)) return new Store();
        try (ObjectInputStream in = new ObjectInputStream(Files.newInputStream(DATA))) { return (Store) in.readObject(); }
    }
    static synchronized void save() throws IOException {
        Files.createDirectories(DATA.toAbsolutePath().getParent());
        Path tmp = DATA.resolveSibling(DATA.getFileName() + ".tmp");
        try (ObjectOutputStream out = new ObjectOutputStream(Files.newOutputStream(tmp))) { out.writeObject(db); }
        try { Files.move(tmp, DATA, StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.ATOMIC_MOVE); }
        catch (AtomicMoveNotSupportedException e) { Files.move(tmp, DATA, StandardCopyOption.REPLACE_EXISTING); }
    }
    static void handle(HttpExchange x) throws IOException {
        try {
            x.getResponseHeaders().set("X-Content-Type-Options", "nosniff");
            x.getResponseHeaders().set("Referrer-Policy", "same-origin");
            x.getResponseHeaders().set("Cache-Control", "no-store");
            if (x.getRequestURI().getPath().startsWith("/api/")) api(x); else staticFile(x);
        } catch (Problem p) { respond(x, p.code, Map.of("error", p.getMessage())); }
        catch (Exception e) { e.printStackTrace(); respond(x, 500, Map.of("error", "Внутренняя ошибка сервера")); }
        finally { x.close(); }
    }
    static void staticFile(HttpExchange x) throws IOException {
        if (!x.getRequestMethod().equals("GET")) throw new Problem(405, "Метод не поддерживается");
        String request = x.getRequestURI().getPath();
        Path file = WEB.resolve(request.equals("/") ? "index.html" : request.substring(1)).normalize();
        if (!file.startsWith(WEB) || !Files.isRegularFile(file)) throw new Problem(404, "Страница не найдена");
        String name = file.getFileName().toString().toLowerCase(Locale.ROOT);
        String type = name.endsWith(".css") ? "text/css" : name.endsWith(".js") ? "application/javascript" : name.endsWith(".png") ? "image/png" : name.endsWith(".jpg") || name.endsWith(".jpeg") ? "image/jpeg" : name.endsWith(".svg") ? "image/svg+xml" : "text/html";
        byte[] bytes = Files.readAllBytes(file);
        x.getResponseHeaders().set("Content-Type", type + "; charset=utf-8");
        x.sendResponseHeaders(200, bytes.length); x.getResponseBody().write(bytes);
    }
    static synchronized void api(HttpExchange x) throws Exception {
        String path = x.getRequestURI().getPath().substring(5);
        String method = x.getRequestMethod();
        Map<String,String> q = params(x.getRequestURI().getRawQuery());
        Map<String,String> f = method.equals("POST") ? body(x) : Map.of();
        User user = current(x);
        if (method.equals("POST")) {
            String origin = x.getRequestHeaders().getFirst("Origin");
            if (origin != null && !origin.equals("http://127.0.0.1:" + x.getLocalAddress().getPort()) && !origin.equals("http://localhost:" + x.getLocalAddress().getPort())) throw new Problem(403, "Недопустимый источник запроса");
            if (!path.equals("login") && !path.equals("register")) {
                Session s = session(x);
                if (s == null || !s.csrf.equals(x.getRequestHeaders().getFirst("X-CSRF-Token"))) throw new Problem(403, "Недействительный токен формы");
            }
        }
        Object result;
        String[] parts = path.split("/");
        if (path.equals("me") && method.equals("GET")) { Session s = session(x); result = Map.of("user", user == null ? "" : userView(user), "csrf", s == null ? "" : s.csrf); }
        else if (path.equals("register") && method.equals("POST")) {
            String email = required(f, "email", 120).toLowerCase(Locale.ROOT);
            if (!email.matches("[^@\\s]+@[^@\\s]+\\.[^@\\s]+")) throw new Problem(400, "Введите корректный email");
            if (db.users.values().stream().anyMatch(u -> u.email.equalsIgnoreCase(email))) throw new Problem(409, "Email уже зарегистрирован");
            String password = required(f, "password", 200); if (password.length() < 8) throw new Problem(400, "Пароль должен содержать не менее 8 символов");
            User u = new User(); u.id = db.nextId++; u.email = email; u.name = required(f,"name",100); u.role = "Visitor"; setPassword(u,password); db.users.put(u.id,u); save(); login(x,u); result = userView(u);
        }
        else if (path.equals("login") && method.equals("POST")) {
            String email = required(f,"email",120); String password = required(f,"password",200);
            User u = db.users.values().stream().filter(a -> a.email.equalsIgnoreCase(email)).findFirst().orElse(null);
            if (u == null || !verify(u,password)) throw new Problem(401,"Неверный email или пароль");
            login(x,u); result = userView(u);
        }
        else if (path.equals("logout") && method.equals("POST")) { String sid = cookie(x); sessions.remove(sid); x.getResponseHeaders().add("Set-Cookie","sid=; HttpOnly; SameSite=Strict; Path=/; Max-Age=0"); result=Map.of("ok",true); }
        else if (path.equals("animals") && method.equals("GET")) {
            result = db.animals.values().stream().filter(a -> matchAnimal(a,q)).sorted(Comparator.comparingLong((Animal a)->a.id).reversed()).map(Main::animalView).toList();
        }
        else if (parts.length == 2 && parts[0].equals("animals") && method.equals("GET")) { Animal a = animal(id(parts[1])); result = animalView(a); }
        else if (path.equals("animals") && method.equals("POST")) { role(user,"Admin","Manager"); Animal a = new Animal(); a.id=db.nextId++; updateAnimal(a,f); a.admitted=LocalDate.now().toString(); db.animals.put(a.id,a); save(); result=animalView(a); }
        else if (parts.length == 2 && parts[0].equals("animals") && method.equals("POST")) { role(user,"Admin","Manager"); Animal a=animal(id(parts[1])); updateAnimal(a,f); save(); result=animalView(a); }
        else if (parts.length == 3 && parts[0].equals("animals") && parts[2].equals("vaccinations") && method.equals("POST")) {
            role(user,"Admin","Manager"); Animal a=animal(id(parts[1])); Vaccination v=new Vaccination(); v.id=db.nextId++; v.name=required(f,"name",120); v.date=date(required(f,"date",20)); a.vaccinations.add(v); save(); result=animalView(a);
        }
        else if (parts.length == 3 && parts[0].equals("animals") && parts[2].equals("medical") && method.equals("POST")) {
            role(user,"Admin","Manager"); Animal a=animal(id(parts[1])); MedicalRecord record=new MedicalRecord(); record.id=db.nextId++; record.date=date(required(f,"date",20)); record.note=required(f,"note",3000); a.medical.add(record); save(); result=animalView(a);
        }
        else if (parts.length == 3 && parts[0].equals("animals") && parts[2].equals("delete") && method.equals("POST")) {
            role(user,"Admin","Manager"); Animal a=animal(id(parts[1]));
            if (db.applications.values().stream().anyMatch(v -> v.animalId==a.id) || db.reviews.values().stream().anyMatch(v -> v.animalId==a.id)) throw new Problem(409,"У животного есть заявки или отзывы; удаление недоступно");
            db.animals.remove(a.id); save(); result=Map.of("ok",true);
        }
        else if (path.equals("applications") && method.equals("GET")) { role(user,"Admin","Manager","Consultant","Visitor"); result=db.applications.values().stream().filter(a -> !user.role.equals("Visitor") || a.userId==user.id).sorted(Comparator.comparingLong((Application a)->a.id).reversed()).map(Main::applicationView).toList(); }
        else if (path.equals("applications") && method.equals("POST")) {
            role(user,"Visitor","Admin","Consultant"); Animal a=animal(id(required(f,"animalId",30)));
            if (a.adopted) throw new Problem(409,"Животное уже пристроено");
            Application ap=new Application(); ap.id=db.nextId++; ap.animalId=a.id; ap.userId=user.id; ap.name=required(f,"name",120); ap.contact=required(f,"contact",200); ap.address=required(f,"address",250); ap.status="SUBMITTED"; ap.created=Instant.now().toString(); history(ap,user,"Заявка подана"); db.applications.put(ap.id,ap); save(); result=applicationView(ap);
        }
        else if (parts.length == 3 && parts[0].equals("applications") && parts[2].equals("status") && method.equals("POST")) {
            role(user,"Admin","Consultant"); Application ap=application(id(parts[1])); String next=required(f,"status",30);
            if (!Set.of("IN_REVIEW","APPROVED","REJECTED","COMPLETED").contains(next)) throw new Problem(400,"Недопустимый статус");
            if (Set.of("REJECTED","COMPLETED").contains(ap.status)) throw new Problem(409,"Заявка уже закрыта");
            if (next.equals("COMPLETED") && !ap.status.equals("APPROVED")) throw new Problem(409,"Сначала одобрите заявку");
            Animal a=animal(ap.animalId); if (a.adopted && !next.equals("REJECTED")) throw new Problem(409,"Животное уже пристроено");
            ap.status=next; history(ap,user, f.getOrDefault("note","").strip());
            if (next.equals("COMPLETED")) { a.adopted=true; a.adoptedAt=LocalDate.now().toString(); db.applications.values().stream().filter(other -> other.animalId==a.id && other.id!=ap.id && !Set.of("COMPLETED","REJECTED").contains(other.status)).forEach(other -> {other.status="REJECTED";history(other,user,"Животное пристроено по другой заявке");}); }
            save(); result=applicationView(ap);
        }
        else if (path.equals("reviews") && method.equals("GET")) { Long animalId=q.containsKey("animalId")?id(q.get("animalId")):null; result=db.reviews.values().stream().filter(r -> r.approved && (animalId==null || r.animalId==animalId)).sorted(Comparator.comparingLong((Review r)->r.id).reversed()).map(Main::reviewView).toList(); }
        else if (path.equals("reviews") && method.equals("POST")) { role(user,"Visitor"); Review r=new Review(); r.id=db.nextId++; r.animalId=id(required(f,"animalId",30)); if(r.animalId!=0)animal(r.animalId); r.userId=user.id; r.content=required(f,"content",2000); r.created=Instant.now().toString(); db.reviews.put(r.id,r); save(); result=reviewView(r); }
        else if (path.equals("moderation") && method.equals("GET")) { role(user,"Admin"); result=db.reviews.values().stream().sorted(Comparator.comparingLong((Review r)->r.id).reversed()).map(Main::reviewView).toList(); }
        else if (parts.length == 3 && parts[0].equals("reviews") && parts[2].equals("moderate") && method.equals("POST")) { role(user,"Admin"); Review r=db.reviews.get(id(parts[1])); if(r==null)throw new Problem(404,"Отзыв не найден"); String action=required(f,"action",20); if(action.equals("approve"))r.approved=true; else if(action.equals("hide"))r.approved=false; else if(action.equals("delete"))db.reviews.remove(r.id); else throw new Problem(400,"Неизвестное действие"); save(); result=Map.of("ok",true); }
        else if (path.equals("users") && method.equals("GET")) { role(user,"Admin"); result=db.users.values().stream().map(Main::userView).toList(); }
        else if (parts.length == 3 && parts[0].equals("users") && parts[2].equals("role") && method.equals("POST")) { role(user,"Admin"); User target=db.users.get(id(parts[1])); if(target==null)throw new Problem(404,"Пользователь не найден"); String newRole=required(f,"role",30); if(!ROLES.contains(newRole))throw new Problem(400,"Неизвестная роль"); if(target.role.equals("Admin") && !newRole.equals("Admin")) guardLastAdmin(); target.role=newRole; save(); result=userView(target); }
        else if (parts.length == 3 && parts[0].equals("users") && parts[2].equals("delete") && method.equals("POST")) { role(user,"Admin"); User target=db.users.get(id(parts[1])); if(target==null)throw new Problem(404,"Пользователь не найден"); if(target.id==user.id)throw new Problem(409,"Нельзя удалить свою учетную запись"); if(target.role.equals("Admin"))guardLastAdmin(); if(db.applications.values().stream().anyMatch(a->a.userId==target.id)||db.reviews.values().stream().anyMatch(r->r.userId==target.id))throw new Problem(409,"У пользователя есть заявки или отзывы"); db.users.remove(target.id); save(); result=Map.of("ok",true); }
        else if (path.equals("reports") && method.equals("GET")) { role(user,"Admin","Manager","Controller"); result=report(q); }
        else throw new Problem(404,"Адрес не найден");
        respond(x,200,result);
    }
    static void guardLastAdmin() { if(db.users.values().stream().filter(u->u.role.equals("Admin")).count()<=1)throw new Problem(409,"Нельзя удалить последнего администратора"); }
    static Map<String,Object> report(Map<String,String> q) {
        LocalDate from=q.containsKey("from")?LocalDate.parse(date(q.get("from"))):LocalDate.now().minusMonths(6);
        LocalDate to=q.containsKey("to")?LocalDate.parse(date(q.get("to"))):LocalDate.now();
        if(from.isAfter(to))throw new Problem(400,"Начальная дата позже конечной");
        String type=q.getOrDefault("type","");
        List<Animal> animals=db.animals.values().stream().filter(a->type.isBlank()||a.type.equals(type)).toList();
        List<Animal> admitted=animals.stream().filter(a->between(a.admitted,from,to)).toList();
        List<Animal> adopted=animals.stream().filter(a->a.adopted && between(a.adoptedAt,from,to)).toList();
        Map<String,Long> byType=animals.stream().collect(Collectors.groupingBy(a->a.type,TreeMap::new,Collectors.counting()));
        Map<String,Long> byGender=animals.stream().collect(Collectors.groupingBy(a->a.gender,TreeMap::new,Collectors.counting()));
        Map<String,Long> byAge=new TreeMap<>(); for(Animal a:animals){String band=a.age<1?"До 1 года":a.age<4?"1–3 года":a.age<8?"4–7 лет":"8+ лет";byAge.merge(band,1L,Long::sum);}
        Map<String,Long> trend=new TreeMap<>(); for(Animal a:adopted)trend.merge(a.adoptedAt.substring(0,7),1L,Long::sum);
        return Map.of("total",animals.size(),"inShelter",animals.stream().filter(a->!a.adopted).count(),"admitted",admitted.size(),"adopted",adopted.size(),"byType",byType,"byGender",byGender,"byAge",byAge,"trend",trend,"from",from.toString(),"to",to.toString());
    }
    static boolean between(String value,LocalDate from,LocalDate to){if(value==null||value.isBlank())return false;LocalDate d=LocalDate.parse(value);return !d.isBefore(from)&&!d.isAfter(to);}
    static boolean matchAnimal(Animal a,Map<String,String> q){
        if(q.getOrDefault("available","false").equals("true")&&a.adopted)return false;
        if(!q.getOrDefault("type","").isBlank()&&!a.type.equals(q.get("type")))return false;
        if(!q.getOrDefault("gender","").isBlank()&&!a.gender.equals(q.get("gender")))return false;
        if(!q.getOrDefault("minAge","").isBlank()&&a.age<ageFilter(q.get("minAge")))return false;
        if(!q.getOrDefault("maxAge","").isBlank()&&a.age>ageFilter(q.get("maxAge")))return false;
        if(!q.getOrDefault("search","").isBlank()) {String s=q.get("search").toLowerCase(Locale.ROOT);if(!(a.name+" "+a.breed+" "+a.color).toLowerCase(Locale.ROOT).contains(s))return false;}
        return true;
    }
    static int ageFilter(String value){try{int age=Integer.parseInt(value);if(age>=0&&age<=100)return age;}catch(NumberFormatException ignored){}throw new Problem(400,"Неверный возраст в фильтре");}
    static void updateAnimal(Animal a,Map<String,String> f){
        a.name=required(f,"name",100); a.type=required(f,"type",60); a.breed=f.getOrDefault("breed","").strip(); a.color=f.getOrDefault("color","").strip();
        a.gender=required(f,"gender",20); if(!Set.of("Мужской","Женский").contains(a.gender))throw new Problem(400,"Укажите пол");
        try {a.age=Integer.parseInt(required(f,"age",3));a.weight=Double.parseDouble(f.getOrDefault("weight","0"));}catch(NumberFormatException e){throw new Problem(400,"Возраст или вес указан неверно");}
        if(a.age<0||a.age>100||a.weight<0||a.weight>1000)throw new Problem(400,"Возраст или вес вне допустимого диапазона");
        a.health=limited(f.getOrDefault("health",""),3000); a.behavior=limited(f.getOrDefault("behavior",""),3000);
        String photo=f.getOrDefault("photo",""); if(!photo.isBlank()){if(photo.length()>7_000_000||!photo.matches("data:image/(png|jpeg|gif|webp);base64,[A-Za-z0-9+/=]+"))throw new Problem(400,"Фото должно быть PNG, JPEG, GIF или WebP до 5 МБ");a.photo=photo;}
    }
    static String limited(String s,int max){if(s.length()>max)throw new Problem(400,"Слишком длинное значение");return s.strip();}
    static Map<String,Object> animalView(Animal a){return Map.ofEntries(Map.entry("id",a.id),Map.entry("name",a.name),Map.entry("type",a.type),Map.entry("breed",a.breed),Map.entry("color",a.color),Map.entry("gender",a.gender),Map.entry("age",a.age),Map.entry("weight",a.weight),Map.entry("health",a.health),Map.entry("behavior",a.behavior),Map.entry("photo",a.photo),Map.entry("admitted",a.admitted),Map.entry("adopted",a.adopted),Map.entry("adoptedAt",a.adoptedAt),Map.entry("demo",a.demo),Map.entry("vaccinations",a.vaccinations.stream().map(v->Map.of("id",v.id,"name",v.name,"date",v.date)).toList()),Map.entry("medical",a.medical.stream().map(v->Map.of("id",v.id,"note",v.note,"date",v.date)).toList()));}
    static Map<String,Object> userView(User u){return Map.of("id",u.id,"name",u.name,"email",u.email,"role",u.role);}
    static Map<String,Object> applicationView(Application a){Animal animal=db.animals.get(a.animalId);return Map.of("id",a.id,"animalId",a.animalId,"animalName",animal==null?"Животное удалено":animal.name,"userId",a.userId,"name",a.name,"contact",a.contact,"address",a.address,"status",a.status,"created",a.created,"history",a.history);}
    static Map<String,Object> reviewView(Review r){User u=db.users.get(r.userId); Animal a=db.animals.get(r.animalId); return Map.of("id",r.id,"animalId",r.animalId,"animalName",r.animalId==0?"О приюте":a==null?"Животное удалено":a.name,"author",u==null?"Посетитель":u.name,"content",r.content,"created",r.created,"approved",r.approved);}
    static void history(Application a,User u,String note){a.history.add(Map.of("at",Instant.now().toString(),"by",u.name,"status",a.status,"note",note));}
    static Animal animal(long id){Animal a=db.animals.get(id);if(a==null)throw new Problem(404,"Животное не найдено");return a;}
    static Application application(long id){Application a=db.applications.get(id);if(a==null)throw new Problem(404,"Заявка не найдена");return a;}
    static long id(String s){try{return Long.parseLong(s);}catch(Exception e){throw new Problem(400,"Неверный идентификатор");}}
    static String date(String s){try{return LocalDate.parse(s).toString();}catch(Exception e){throw new Problem(400,"Неверная дата");}}
    static String required(Map<String,String> f,String key,int max){String s=f.getOrDefault(key,"").strip();if(s.isBlank()||s.length()>max)throw new Problem(400,"Проверьте поле: "+key);return s;}
    static void role(User u,String... allowed){if(u==null)throw new Problem(401,"Войдите в аккаунт");if(!Arrays.asList(allowed).contains(u.role))throw new Problem(403,"Недостаточно прав");}
    static Map<String,String> body(HttpExchange x)throws IOException {int length=0;ByteArrayOutputStream out=new ByteArrayOutputStream();byte[] buf=new byte[8192];int n;while((n=x.getRequestBody().read(buf))!=-1){length+=n;if(length>7_500_000)throw new Problem(413,"Запрос слишком большой");out.write(buf,0,n);}return params(out.toString(StandardCharsets.UTF_8));}
    static Map<String,String> params(String raw){Map<String,String> result=new HashMap<>();if(raw==null||raw.isBlank())return result;for(String part:raw.split("&")){String[] pair=part.split("=",2);result.put(URLDecoder.decode(pair[0],StandardCharsets.UTF_8),URLDecoder.decode(pair.length>1?pair[1]:"",StandardCharsets.UTF_8));}return result;}
    static void login(HttpExchange x,User u){String sid=token();sessions.put(sid,new Session(u.id,token(),Instant.now().plus(Duration.ofHours(12))));x.getResponseHeaders().add("Set-Cookie","sid="+sid+"; HttpOnly; SameSite=Strict; Path=/; Max-Age=43200");}
    static String cookie(HttpExchange x){String header=x.getRequestHeaders().getFirst("Cookie");if(header==null)return "";for(String c:header.split(";")){String s=c.strip();if(s.startsWith("sid="))return s.substring(4);}return "";}
    static Session session(HttpExchange x){Session s=sessions.get(cookie(x));if(s!=null&&s.expires.isAfter(Instant.now()))return s;return null;}
    static User current(HttpExchange x){Session s=session(x);return s==null?null:db.users.get(s.userId);}
    static String token(){byte[] b=new byte[32];random.nextBytes(b);return Base64.getUrlEncoder().withoutPadding().encodeToString(b);}
    static void setPassword(User u,String password){byte[] salt=new byte[16];random.nextBytes(salt);u.salt=Base64.getEncoder().encodeToString(salt);u.hash=hash(password,salt);}
    static boolean verify(User u,String password){return MessageDigest.isEqual(Base64.getDecoder().decode(u.hash),Base64.getDecoder().decode(hash(password,Base64.getDecoder().decode(u.salt))));}
    static String hash(String password,byte[] salt){try{PBEKeySpec spec=new PBEKeySpec(password.toCharArray(),salt,120_000,256);return Base64.getEncoder().encodeToString(SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256").generateSecret(spec).getEncoded());}catch(Exception e){throw new IllegalStateException(e);}}
    static void respond(HttpExchange x,int code,Object value)throws IOException{byte[] bytes=json(value).getBytes(StandardCharsets.UTF_8);x.getResponseHeaders().set("Content-Type","application/json; charset=utf-8");x.sendResponseHeaders(code,bytes.length);x.getResponseBody().write(bytes);}
    static String json(Object v){if(v==null)return"null";if(v instanceof Boolean||v instanceof Number)return v.toString();if(v instanceof Map<?,?> m)return m.entrySet().stream().map(e->json(String.valueOf(e.getKey()))+":"+json(e.getValue())).collect(Collectors.joining(",","{","}"));if(v instanceof Iterable<?> it){List<String> parts=new ArrayList<>();it.forEach(a->parts.add(json(a)));return String.join(",",parts).transform(s->"["+s+"]");}String s=v.toString();StringBuilder b=new StringBuilder("\"");for(char c:s.toCharArray()){switch(c){case '"'->b.append("\\\"");case '\\'->b.append("\\\\");case '\n'->b.append("\\n");case '\r'->b.append("\\r");case '\t'->b.append("\\t");default->{if(c<32)b.append(String.format("\\u%04x",(int)c));else b.append(c);}}}return b.append('"').toString();}
    static class Problem extends RuntimeException {final int code;Problem(int code,String message){super(message);this.code=code;}}
    /** Small HTTP/1.1 adapter that avoids the JDK selector used by HttpServer. */
    static class SimpleExchange extends HttpExchange {
        final Socket socket;
        final Headers requestHeaders = new Headers(), responseHeaders = new Headers();
        final URI uri;
        final String method;
        InputStream requestBody;
        final ByteArrayOutputStream responseBody = new ByteArrayOutputStream();
        int responseCode = 200;
        boolean closed;
        SimpleExchange(Socket socket) throws IOException {
            this.socket = socket;
            socket.setSoTimeout(15_000);
            BufferedInputStream input = new BufferedInputStream(socket.getInputStream());
            String line = readLine(input);
            String[] first = line.split(" ", 3);
            if (first.length < 3 || !first[2].startsWith("HTTP/1.")) throw new IOException("Invalid HTTP request");
            method = first[0]; uri = URI.create(first[1]);
            int headerCount = 0;
            while (!(line = readLine(input)).isEmpty()) {
                if (++headerCount > 100) throw new IOException("Too many headers");
                int colon = line.indexOf(':');
                if (colon <= 0) throw new IOException("Invalid header");
                requestHeaders.add(line.substring(0, colon), line.substring(colon + 1).strip());
            }
            int length;
            try { length = Integer.parseInt(requestHeaders.getFirst("Content-Length") == null ? "0" : requestHeaders.getFirst("Content-Length")); }
            catch (NumberFormatException e) { throw new IOException("Invalid body length", e); }
            if (length < 0 || length > 7_500_000) throw new IOException("Body too large");
            requestBody = new ByteArrayInputStream(input.readNBytes(length));
        }
        static String readLine(InputStream input) throws IOException {
            ByteArrayOutputStream b = new ByteArrayOutputStream(); int c;
            while ((c = input.read()) != -1 && c != '\n') { if (b.size() >= 8192) throw new IOException("Line too long"); if (c != '\r') b.write(c); }
            if (c == -1) throw new EOFException();
            return b.toString(StandardCharsets.ISO_8859_1);
        }
        public Headers getRequestHeaders(){return requestHeaders;}
        public Headers getResponseHeaders(){return responseHeaders;}
        public URI getRequestURI(){return uri;}
        public String getRequestMethod(){return method;}
        public HttpContext getHttpContext(){return null;}
        public InputStream getRequestBody(){return requestBody;}
        public OutputStream getResponseBody(){return responseBody;}
        public void sendResponseHeaders(int code,long length){responseCode=code;}
        public InetSocketAddress getRemoteAddress(){return (InetSocketAddress) socket.getRemoteSocketAddress();}
        public int getResponseCode(){return responseCode;}
        public InetSocketAddress getLocalAddress(){return (InetSocketAddress) socket.getLocalSocketAddress();}
        public String getProtocol(){return "HTTP/1.1";}
        public Object getAttribute(String key){return null;}
        public void setAttribute(String key,Object value){}
        public void setStreams(InputStream input,OutputStream output){requestBody=input;}
        public HttpPrincipal getPrincipal(){return null;}
        public void close(){
            if (closed) return; closed=true;
            try {
                byte[] body=responseBody.toByteArray();
                OutputStream out=socket.getOutputStream();
                out.write(("HTTP/1.1 "+responseCode+" "+reason(responseCode)+"\r\n").getBytes(StandardCharsets.US_ASCII));
                responseHeaders.set("Content-Length",String.valueOf(body.length));
                responseHeaders.set("Connection","close");
                for(Map.Entry<String,List<String>> h:responseHeaders.entrySet()) for(String value:h.getValue()) out.write((h.getKey()+": "+value+"\r\n").getBytes(StandardCharsets.ISO_8859_1));
                out.write("\r\n".getBytes(StandardCharsets.US_ASCII));
                out.write(body); out.flush();
            } catch(IOException ignored) {} finally { try { socket.close(); } catch(IOException ignored) {} }
        }
        static String reason(int code){return switch(code){case 200->"OK";case 400->"Bad Request";case 401->"Unauthorized";case 403->"Forbidden";case 404->"Not Found";case 405->"Method Not Allowed";case 409->"Conflict";case 413->"Content Too Large";default->"Internal Server Error";};}
    }
    record Session(long userId,String csrf,Instant expires){}
    static class Store implements Serializable {private static final long serialVersionUID=1L;long nextId=1;boolean demoSeeded;Map<Long,User> users=new LinkedHashMap<>();Map<Long,Animal> animals=new LinkedHashMap<>();Map<Long,Application> applications=new LinkedHashMap<>();Map<Long,Review> reviews=new LinkedHashMap<>();}
    static class User implements Serializable {private static final long serialVersionUID=1L;long id;String name,email,role,salt,hash;}
    static class Animal implements Serializable {private static final long serialVersionUID=1L;long id;String name="",type="",breed="",color="",gender="",health="",behavior="",photo="",admitted="",adoptedAt="";int age;double weight;boolean adopted,demo;List<Vaccination> vaccinations=new ArrayList<>();List<MedicalRecord> medical=new ArrayList<>();}
    static class Vaccination implements Serializable {private static final long serialVersionUID=1L;long id;String name,date;}
    static class MedicalRecord implements Serializable {private static final long serialVersionUID=1L;long id;String date,note;}
    static class Application implements Serializable {private static final long serialVersionUID=1L;long id,animalId,userId;String name,contact,address,status,created;List<Map<String,String>> history=new ArrayList<>();}
    static class Review implements Serializable {private static final long serialVersionUID=1L;long id,animalId,userId;String content,created;boolean approved;}
}
