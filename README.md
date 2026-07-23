# bible2ppt

성경 구절을 Microsoft PowerPoint 슬라이드로 만드는 Windows 앱과, 같은 내장 데이터를
조회하는 ASP.NET Core 웹 API를 함께 관리하는 저장소입니다.

## 저장소 지도

- `Bible2PPT/Bible2PPT.sln`: 캐노니컬 .NET 솔루션
- `Bible2PPT/Bible2PPT/`: Windows Forms 데스크톱 앱
- `Bible2PPT/Bible2PPT.Web/`: 솔루션과 Docker가 사용하는 웹 API
- `Bible2PPT/Bible2PPT.Services.*/`: 성경·인덱스·PPT 빌드·템플릿 서비스
- `Bible2PPT/bible.txt`: 앱과 API가 공유하는 내장 데이터
- `Bible2PPT/README.md`: 사용자 기능, 구절 문법, 템플릿 설명

루트의 `Bible2PPT.Web/`는 솔루션 밖 호환 사본입니다. 새 기능의 정본은
`Bible2PPT/Bible2PPT.Web/`이며, 두 사본을 독립적으로 발전시키지 않습니다.

## 빠른 시작

저장소 루트에서 .NET 6 타깃을 지원하는 SDK와 .NET 6 런타임으로 실행합니다.

```bash
dotnet restore Bible2PPT/Bible2PPT.Web/Bible2PPT.Web.csproj
dotnet run --project Bible2PPT/Bible2PPT.Web/Bible2PPT.Web.csproj
```

기본 주소는 `http://localhost:5000`이고 상태 확인은 `/healthz`, API 문서는 `/swagger`에서
볼 수 있습니다.

```bash
dotnet test Bible2PPT/Bible2PPT.Services.BibleIndexService.Tests/Bible2PPT.Services.BibleIndexService.Tests.csproj -c Release
docker build -t bible2ppt .
docker run --rm -p 8080:8080 bible2ppt
```

Windows 데스크톱 앱과 전체 통합 테스트에는 Windows 및 Microsoft PowerPoint가 필요합니다.
개발·검증 범위와 안전 규칙은 `AGENTS.md`를 확인하세요.
