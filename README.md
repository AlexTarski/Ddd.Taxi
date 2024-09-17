# DDDTaxi

TaxiOrder и TaxiApi — это модель предметной области по заказу такси.

TaxiOrder — типичная анемичная модель. Вся логика тесно связана с TaxiApi.

PersonName, Address, Driver, Car - вспомогательные классы, группирующие связанные свойства внутри себя.

При подходе DDD работой с базой данных занимаются репозитории. Задача репозитория читать и записывать данные в БД и ничего более.
Репозиторий водителей реализована исходя из той логики, что он ничего недолжен знать про заказы.

В TaxiApi оставлен только тестовый код и вызовы соответствующих методов TaxiOrder.

Добавлены проверки на валидность действий: InvalidOperationException, если действие в данном состоянии заказа невалидно.