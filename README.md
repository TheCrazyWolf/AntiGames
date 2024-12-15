### Киллер процессов по ключевым словам, названиям

Убивает процессы если находятся запрещенные слова в заголовках окон,
запрещенные назывании программ - на определенные учетные записи

```
sc create KillerProcess binPath="C:\Program Files\ProcessKiller\KillerProcess.exe" DisplayName="KillerProcess" type=own start=auto
```

```
sc start KillerProcess
```
