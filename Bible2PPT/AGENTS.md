> 저장소 캐노니컬 규칙: 상위 `../AGENTS.md`를 먼저 따른다.

# `Bible2PPT/` 소스 트리 보충 규칙

- `Bible2PPT.sln`이 데스크톱 앱, 서비스, 웹 API, 테스트를 묶는 캐노니컬 솔루션이다.
- 이 디렉터리의 `.editorconfig`를 C#·프로젝트 파일에 적용한다.
- `bible.txt`는 앱과 API가 공유하는 내장 데이터 정본이다. 출처와 파서 영향을 확인하지 않은
  대량 변경·재생성을 금지한다.
- `Bible2PPT.Web/`가 솔루션과 Docker가 빌드하는 웹 API 정본이다. 저장소 루트의 동명
  호환 사본만 단독 수정하지 않는다.
- `Bible2PPT.Services.BuildService/`와 `Bible2PPT.Tests/`는 Windows 및 PowerPoint COM에
  의존한다. 플랫폼 제약은 상위 규칙의 검증 절차에 따라 분리해 보고한다.
- EF Core `Migrations/`를 모델 변경 없이 수동으로 맞춰 쓰거나, 생성 DB를 소스처럼
  커밋하지 않는다.
