> 공통 정책: 워크스페이스 루트 `../AGENTS.md` (ruahverce 정책)를 먼저 따른다.
> 단독 클론에서는 최소한 비밀정보 커밋 금지, 파괴적 작업 사전 승인, 실패 게이트 우회 금지,
> 에이전트별 브랜치·worktree 격리를 동일하게 적용한다.

# bible2ppt 작업 규칙

## 1. 목적과 실행 환경

이 저장소는 성경 구절을 Microsoft PowerPoint 슬라이드로 만드는 Windows 데스크톱 앱과,
동일한 내장 성경 데이터를 조회하는 ASP.NET Core 웹 API를 함께 제공한다.

- 언어/런타임: C# · .NET 6 (지원 종료 상태이므로 업그레이드는 별도 호환성 작업으로 다룬다)
- 저장소의 캐노니컬 솔루션: `Bible2PPT/Bible2PPT.sln`
- 데스크톱 앱: Windows Forms + Microsoft PowerPoint COM 자동화
- 웹 API: ASP.NET Core Minimal API + SQLite, Linux/Docker 실행 가능
- 테스트: xUnit

## 2. 구조 지도

| 경로 | 역할 |
|---|---|
| `Bible2PPT/Bible2PPT/` | Windows Forms UI와 앱 설정 |
| `Bible2PPT/Bible2PPT.Services.BibleService/` | 성경 데이터, 오프라인 시드, EF Core 컨텍스트 |
| `Bible2PPT/Bible2PPT.Services.BibleIndexService/` | 책/장/절 인덱스와 구절 질의 파서 |
| `Bible2PPT/Bible2PPT.Services.BuildService/` | PowerPoint COM 기반 PPT 생성 |
| `Bible2PPT/Bible2PPT.Services.TemplateService/` | PPT 템플릿 메타데이터 |
| `Bible2PPT/Bible2PPT.Web/` | 캐노니컬 웹 API와 정적 UI; 솔루션·Docker 빌드 대상 |
| `Bible2PPT/Bible2PPT.Services.BibleIndexService.Tests/` | 플랫폼 독립 인덱스/파서 단위 테스트 |
| `Bible2PPT/Bible2PPT.Tests/` | Windows·PowerPoint가 필요한 통합 테스트 |
| `Bible2PPT/bible.txt` | 데스크톱 앱과 API가 공유하는 내장 성경 데이터 |
| `Bible2PPT/Template/` | PPT 템플릿의 압축 해제 구조 |
| `Bible2PPT.Web/` | 솔루션 밖 호환 사본; 새 API 작업의 정본이 아님 |
| `Dockerfile` | 캐노니컬 웹 API를 게시하는 컨테이너 빌드 |

`Bible2PPT.Web/`와 `Bible2PPT/Bible2PPT.Web/`에 같은 API 코드가 남아 있다. 솔루션과
Dockerfile은 안쪽 프로젝트를 사용하므로 기능 변경은 안쪽을 정본으로 삼는다. 바깥 호환
사본을 유지해야 하는 변경이라면 두 사본의 차이를 명시적으로 확인한다.

## 3. 실행과 결정론적 검증

저장소 루트에서 실행한다. .NET 6 타깃을 빌드할 수 있는 SDK와 테스트 실행용 .NET 6
런타임이 필요하다. 상위 SDK만 설치된 환경은 빌드가 성공해도 `testhost`가 시작되지 않을 수 있다.

```bash
# 빠른 플랫폼 독립 게이트
dotnet restore Bible2PPT/Bible2PPT.Web/Bible2PPT.Web.csproj
dotnet build Bible2PPT/Bible2PPT.Web/Bible2PPT.Web.csproj -c Release --no-restore
dotnet test Bible2PPT/Bible2PPT.Services.BibleIndexService.Tests/Bible2PPT.Services.BibleIndexService.Tests.csproj -c Release

# API 수동 실행
dotnet run --project Bible2PPT/Bible2PPT.Web/Bible2PPT.Web.csproj

# 컨테이너 검증
docker build -t bible2ppt .
docker run --rm -p 8080:8080 bible2ppt
```

전체 솔루션의 COM 참조는 `dotnet build`의 Core MSBuild로 해결되지 않는다. Windows에서
Microsoft PowerPoint, Visual Studio의 .NET 데스크톱 빌드 도구, Windows SDK를 설치한 뒤
**Developer PowerShell for Visual Studio**에서 검증한다.

```powershell
msbuild Bible2PPT/Bible2PPT.sln -restore -p:Configuration=Release
dotnet test Bible2PPT/Bible2PPT.sln -c Release --no-build
```

PowerPoint COM 프로젝트와 `Bible2PPT.Tests`의 실패를 Linux, PowerPoint 미설치, Windows
SDK 도구 미설치 환경의 제품 회귀로 단정하지 않는다. 대신 플랫폼 독립 게이트 결과와 실행
환경 제약을 함께 보고한다.

## 4. 데이터와 생성물

- `Bible2PPT/bible.txt`가 내장 데이터의 정본이다. 출처·라이선스·파서 영향을 확인하지 않고
  대량 수정하거나 재생성하지 않는다.
- 웹 API는 실행 파일 옆 `data/`에 `bible-v3.db`, `bindex-v3.db`를 만든다. 데스크톱 앱도
  실행 위치에 SQLite DB를 생성할 수 있다. DB, `bin/`, `obj/`, `TestResults/`는 커밋하지 않는다.
- EF Core `Migrations/`는 추적되는 스키마 이력이다. 모델 변경 없이 수동으로 맞춰 쓰지 않는다.
- `Bible2PPT/Template/`와 `Bible2PPT/Bible2PPT/Resources/Template.pptx` 변경은 실제 PPT
  생성 결과를 Windows에서 확인한다.

## 5. 코드와 테스트 원칙

- `Bible2PPT/.editorconfig`의 4칸 들여쓰기, UTF-8 BOM, `System.*` 우선 using 정렬을 따른다.
- 공개 타입/멤버는 `PascalCase`, 지역 변수/매개변수는 `camelCase`를 유지한다.
- 구절 파서나 인덱스 규칙 변경에는 `BibleIndexService.Tests` 회귀 테스트를 추가한다.
- 웹 엔드포인트 변경 시 `/healthz`와 영향받은 `/api/*` 응답을 수동 또는 자동으로 확인한다.
- Windows UI/PPT 변경은 빌드 결과뿐 아니라 생성된 PPT 또는 UI 동작도 확인해 보고한다.

## 6. 보안·고유 금지사항

- 키·토큰·실제 connection string을 코드나 설정에 하드코딩하지 않는다. 로컬 값은 추적되지
  않는 `.env` 또는 런타임 환경 변수로 주입하고 예시는 `.env.example`만 사용한다.
- GitHub Actions의 `${{ secrets.* }}` 참조를 실제 값으로 바꾸거나 로그에 출력하지 않는다.
- 생성 DB, 사용자 PPT, Office 임시 파일, 빌드 산출물을 커밋하지 않는다.
- 바깥 `Bible2PPT.Web/`만 수정해 솔루션·Docker 대상과 분기시키지 않는다.
- PowerPoint COM 의존 코드를 플랫폼 독립 서비스/API 계층에 추가하지 않는다.
- 실패하는 테스트를 삭제·skip하거나 검증 명령을 약화해 통과시키지 않는다.
- 삭제, `reset --hard`, force push, 대량 데이터 재생성은 사용자 승인 없이 하지 않는다.
- 문서·설정에 개인 절대경로를 넣지 않는다.

## 7. 에이전트·스킬 운영

- 에이전트 1명당 브랜치 1개, `~/worktrees/` 아래 worktree 1개를 사용한다.
- 커밋 전 `git status`와 `git log --oneline -5`로 낯선 변경을 확인하고, 명시한 파일만 stage한다.
- 이 저장소에는 로컬 스킬의 캐노니컬 사본이 없다. 공유 스킬을 `.claude/`·`.codex/`에
  복사해 새 중복 소스를 만들지 않는다.
