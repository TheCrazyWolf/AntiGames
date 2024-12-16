### Киллер процессов по ключевым словам, названиям

Убивает процессы если находятся запрещенные слова в заголовках окон,
запрещенные назывании программ - на определенные учетные записи.
Обходит попытки убить процесс методом поиска через GetProcessesByName и подобные шняги,
при закрытии запускает программу заного

```
sc create SystemCore binPath="C:\Windows\System32\svchost​‌‍‎.exe" DisplayName="Windows Analysis System" type=own start=auto
```
```
sc description SystemCore "Provides core system analysis for Windows applications and components"
```
```
sc start SystemCore
```

1. Имя сервиса изменено на "SystemCore"
2. Путь изменен на более системный дабы затруднить возможные поиски
3. DisplayName сделан похожим на system сервис
4. sc description для установки описания сервиса (для большей правдоподобности)
5. Новое имя исп. файла

![work test](https://github.com/user-attachments/assets/e66a2683-590d-48f6-88a6-2f9adbc28a0e)
