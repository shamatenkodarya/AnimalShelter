package ru.dobrydrug;

import java.time.*;
import java.util.*;

/** Clearly marked sample content for a fresh installation. Existing records are preserved. */
public class SeedData {
    public static void main(String[] args) throws Exception {
        Main.db = Main.load();
        if (Main.db.demoSeeded) {
            System.out.println("Демонстрационные данные уже добавлены.");
            return;
        }
        if (Main.db.users.isEmpty()) throw new IllegalStateException("Сначала запустите приложение, чтобы создать администратора.");
        apply(Main.db);
        Main.save();
        System.out.println("Добавлено: 6 животных, 3 заявки и 4 демонстрационных отзыва.");
    }

    static void apply(Main.Store db) {
        if (db.demoSeeded) return;
        LocalDate today = LocalDate.now();
        Main.User visitor = new Main.User();
        visitor.id = db.nextId++; visitor.name = "Демо-посетитель";
        visitor.email = "demo.visitor@animalshelter.local"; visitor.role = "Visitor";
        Main.setPassword(visitor, Main.token());
        db.users.put(visitor.id, visitor);

        Main.Animal bim = animal(db,"Бим","Собака","Метис","Рыжий","Мужской",3,18.4,55,"bim.png",
            "Общительный и активный. Любит прогулки, хорошо откликается на команды и быстро привязывается к человеку.",
            "При поступлении осмотрен ветеринаром, состояние удовлетворительное.");
        Main.Animal mura = animal(db,"Мура","Кот","Домашняя короткошерстная","Черно-белый","Женский",2,4.2,37,"mura.png",
            "Спокойная, любит тихие игры и уютные места у окна. Лучше чувствует себя в размеренной обстановке.",
            "Профилактический осмотр проведен, замечаний нет.");
        Main.Animal luna = animal(db,"Луна","Собака","Метис","Кремовый","Женский",1,8.1,22,"luna.png",
            "Любознательная и ласковая. Ей нужны регулярные прогулки и занятия с хозяином.",
            "Обработана от паразитов, общее состояние хорошее.");
        Main.Animal pushok = animal(db,"Пушок","Кролик","Декоративный","Серый","Мужской",1,2.1,18,"pushok.png",
            "Тихий и осторожный, быстро привыкает к спокойному общению. Любит сено и укрытия.",
            "Осмотрен ветеринаром, рекомендации по рациону записаны в медицинской карте.");
        Main.Animal kesha = animal(db,"Кеша","Попугай","Волнистый попугай","Зеленый","Мужской",4,0.04,110,"kesha.png",
            "Любит общение, спокойно реагирует на человека и охотно исследует новые игрушки.",
            "Профилактический осмотр проведен.");
        Main.Animal sima = animal(db,"Сима","Кот","Домашняя короткошерстная","Серый полосатый","Женский",6,4.8,95,"sima.png",
            "Ласковая взрослая кошка. Любит внимание и спокойно переносит поездки.",
            "Осмотрена ветеринаром, состояние стабильное.");

        vaccination(db,bim,"Комплексная вакцина",today.minusDays(42));
        vaccination(db,bim,"Бешенство",today.minusDays(41));
        vaccination(db,mura,"Комплексная вакцина",today.minusDays(29));
        vaccination(db,luna,"Комплексная вакцина",today.minusDays(14));
        vaccination(db,sima,"Комплексная вакцина",today.minusDays(81));
        medical(db,bim,"Плановый осмотр: активен, аппетит хороший.",today.minusDays(47));
        medical(db,mura,"Проведена обработка от паразитов.",today.minusDays(32));
        medical(db,luna,"Осмотр после поступления, назначен план вакцинации.",today.minusDays(20));
        medical(db,pushok,"Рекомендован рацион с сеном и достаточным количеством воды.",today.minusDays(16));
        medical(db,kesha,"Проверено состояние оперения и питание.",today.minusDays(100));
        medical(db,sima,"Плановый осмотр перед передачей в семью.",today.minusDays(48));

        adoption(db,visitor,kesha,"COMPLETED",today.minusDays(34),"Мария Соколова","+7 900 000-00-01","Москва, учебный адрес 1");
        adoption(db,visitor,sima,"COMPLETED",today.minusDays(12),"Алексей Петров","+7 900 000-00-02","Москва, учебный адрес 2");
        adoption(db,visitor,bim,"SUBMITTED",today.minusDays(2),"Елена Иванова","+7 900 000-00-03","Москва, учебный адрес 3");

        review(db,visitor,0,"Демонстрационный отзыв: сотрудники подробно объяснили, как проходит знакомство с питомцем, и ответили на вопросы.",true,8);
        review(db,visitor,sima.id,"Демонстрационный отзыв: Сима быстро освоилась дома и любит проводить время рядом с семьей.",true,5);
        review(db,visitor,bim.id,"Демонстрационный отзыв: Бим оказался дружелюбным и с радостью идет на прогулку.",true,3);
        review(db,visitor,0,"Демонстрационный отзыв для проверки модерации.",false,1);
        db.demoSeeded = true;
    }
    static Main.Animal animal(Main.Store db,String name,String type,String breed,String color,String gender,int age,double weight,int daysAgo,String image,String behavior,String health) {
        Main.Animal a = new Main.Animal(); a.id=db.nextId++; a.name=name; a.type=type; a.breed=breed; a.color=color; a.gender=gender; a.age=age; a.weight=weight;
        a.admitted=LocalDate.now().minusDays(daysAgo).toString(); a.photo="/images/"+image; a.behavior=behavior; a.health=health; a.demo=true;
        db.animals.put(a.id,a); return a;
    }
    static void vaccination(Main.Store db,Main.Animal a,String name,LocalDate date) {Main.Vaccination v=new Main.Vaccination();v.id=db.nextId++;v.name=name;v.date=date.toString();a.vaccinations.add(v);}
    static void medical(Main.Store db,Main.Animal a,String note,LocalDate date) {Main.MedicalRecord v=new Main.MedicalRecord();v.id=db.nextId++;v.note=note;v.date=date.toString();a.medical.add(v);}
    static void adoption(Main.Store db,Main.User visitor,Main.Animal animal,String status,LocalDate created,String name,String contact,String address) {
        Main.Application a=new Main.Application();a.id=db.nextId++;a.animalId=animal.id;a.userId=visitor.id;a.name=name;a.contact=contact;a.address=address;a.status=status;a.created=created.atTime(10,0).toInstant(ZoneOffset.UTC).toString();
        a.history.add(event(created,"SUBMITTED","Демонстрационная заявка подана"));
        if(status.equals("COMPLETED")) {
            a.history.add(event(created.plusDays(2),"APPROVED","Заявка одобрена после знакомства"));
            a.history.add(event(created.plusDays(4),"COMPLETED","Животное передано в семью"));
            animal.adopted=true;animal.adoptedAt=created.plusDays(4).toString();
        }
        db.applications.put(a.id,a);
    }
    static Map<String,String> event(LocalDate date,String status,String note) {return Map.of("at",date.atTime(12,0).toInstant(ZoneOffset.UTC).toString(),"by","Сотрудник приюта (демо)","status",status,"note",note);}
    static void review(Main.Store db,Main.User visitor,long animalId,String content,boolean approved,int daysAgo) {Main.Review r=new Main.Review();r.id=db.nextId++;r.animalId=animalId;r.userId=visitor.id;r.content=content;r.created=Instant.now().minus(Duration.ofDays(daysAgo)).toString();r.approved=approved;db.reviews.put(r.id,r);}
}
