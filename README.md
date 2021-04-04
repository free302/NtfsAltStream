# NtfsAltStream
NTFS Alternate Stream 조작 라이브러리 및 테스트

- [x] `Lib` : library, win32 api
- [x] `Tester` : test app, WinForms
  - 테스트는 `WindowsService1.MyTask.Run()` 실행
  - 파일에 대한 실제 작업은 `MyTask.doSomthingWithTheFile()`에서 수행
- [x] `Service` : test app, Windows service 형태  

### TODOs
- [ ] delete stream 메소드 추가
