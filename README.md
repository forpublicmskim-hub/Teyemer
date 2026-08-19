# Teyemer

Windows 트레이에서 일정 주기로 눈 휴식을 안내하는 .NET 10 LTS WPF 앱입니다. 관리자 권한과 서버가 필요 없으며 런타임 외부 패키지를 사용하지 않습니다.

## 실행과 테스트

```powershell
dotnet build Teyemer.slnx
dotnet run --project .\src\Teyemer.App\Teyemer.App.csproj
dotnet run --project .\tests\Teyemer.Tests\Teyemer.Tests.csproj
dotnet publish .\src\Teyemer.App\Teyemer.App.csproj -p:PublishProfile=SingleFile
```

설정은 `%LocalAppData%\Teyemer\settings.json`에 저장되며 손상된 파일은 안전한 기본값으로 복구합니다.

## 구조와 기능

- `src/Teyemer.Core`: WPF 독립 일정 판정, 설정, 단일 알림 상태 머신 소스 모듈
- `src/Teyemer.Infrastructure`: 원자적 JSON 저장과 현재 사용자 HKCU Run 자동 실행 소스 모듈
- `src/Teyemer.App`: 위 모듈을 직접 포함해 하나의 앱 어셈블리로 컴파일하는 MVVM/WPF 실행 프로젝트
- `tests/Teyemer.Tests`: 일정 경계, 상태 전이, 설정 복구, 명령 인용 테스트

Core와 Infrastructure는 별도 프로젝트/DLL이 아니라 논리적으로 분리된 소스 모듈입니다. `SingleFile` 게시 프로필은 .NET 런타임과 모든 관리 코드를 묶어 `artifacts\publish\win-x64\Teyemer.App.exe` 하나만 생성합니다. 일반 Debug 빌드는 .NET 개발 모델상 `Teyemer.App.dll`을 포함하므로 다른 PC에 전달할 때는 게시 폴더의 단일 EXE를 사용합니다.

트레이 메뉴는 상태/남은 시간, 즉시 운동, 30분·오늘 일시정지, 설정/안내, 종료를 제공합니다. 요일별 활성 시간과 자정 통과, 5분 다시 알림, 오른쪽 아래 팝업·시스템음, 운동 카운트다운을 지원합니다. 일반 설정에서 청회색 기반 다크 모드와 알림 자동 닫힘 시간(1~60초, 기본 30초)을 설정할 수 있습니다. Windows 11에서는 메인 창에 Mica, 운동 창에 transient system backdrop을 적용하고 모든 콘텐츠를 반투명 유리 카드로 표시합니다. 지원되지 않는 환경에서는 동일 팔레트의 불투명 배경으로 안전하게 대체됩니다. 알림은 자동 또는 사용자 동작으로 닫힐 때 페이드아웃됩니다. 잠금·절전과 10분 사용자 부재 중 알림을 억제하고 복귀 시 새 주기를 시작합니다.

## 제한사항

실제 트레이, 잠금/절전, HKCU 등록과 시스템 음향은 대화형 Windows 세션에서 수동 확인해야 합니다. 전체 화면/프레젠테이션 감지는 신뢰성 문제로 제외했습니다. 후속 버전에서는 `SHQueryUserNotificationState`를 보조 신호로 사용하고 사용자 재정의를 제공하는 방식을 권장합니다. 부재 기준은 현재 10분 고정이며 다중 모니터 팝업 위치와 접근성 자동화도 후속 보강 대상입니다.

## Smart App Control과 코드 서명

Windows 11 Smart App Control이 켜진 PC에서는 알려지지 않은 미서명 Debug/Release 산출물이 Code Integrity 오류 3033/3077 또는 `0x800711C7`로 차단될 수 있습니다. 파일 합치기나 self-contained 게시만으로는 해결되지 않습니다. 배포본은 신뢰된 공급자가 발급한 RSA 코드 서명 인증서로 Authenticode 서명해야 합니다. 자체 서명 테스트 인증서는 Smart App Control 배포 요구사항을 충족하지 않습니다.

신뢰된 코드 서명 인증서가 현재 사용자의 인증서 저장소에 설치된 후 다음 명령으로 서명된 단일 파일 배포본을 만듭니다.

```powershell
.\scripts\Publish-Signed.ps1 -CertificateThumbprint '<40자리 인증서 지문>'
```

결과는 `artifacts\publish\signed-win-x64\Teyemer.App.exe`에 생성되며 스크립트가 SHA-256 서명과 타임스탬프를 적용한 뒤 SignTool로 검증합니다. 조직 관리 PC에서는 별도로 관리자가 App Control 허용 정책을 배포할 수도 있습니다. Smart App Control 자체를 끄는 것은 이 프로젝트의 해결 절차로 권장하지 않습니다.
