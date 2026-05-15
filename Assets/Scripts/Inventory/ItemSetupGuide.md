# Item Setup Guide

아트에서 아이템 이미지를 받으면 코드 수정 없이 아래 순서로 연결하면 됩니다.

1. 아이템 PNG를 `Assets/Inventory/Sprites` 또는 원하는 `Sprites` 폴더에 넣습니다.
2. Unity Inspector에서 이미지의 `Texture Type`을 `Sprite (2D and UI)`로 설정하고 `Apply`를 누릅니다.
3. 씬의 `Inventory System` 오브젝트를 선택합니다.
4. `Inventory` 컴포넌트의 `Pickup Items` 목록에서 해당 아이템 항목을 펼칩니다.
5. 각 항목에서 아래 값을 직접 수정합니다.
   - `Id`: 퍼즐에서 쓰는 고정 이름입니다. 예: `key`, `diary`, `file`
   - `Display Name`: 획득 팝업 제목에 보일 이름입니다. 예: `열쇠`
   - `Description`: 획득 팝업 아래에 보일 설명입니다. 예: `책상 서랍을 열 수 있을 것 같다.`
   - `Scene Object`: 씬에 놓인 실제 클릭/드래그 대상 오브젝트입니다.
   - `Icon`: 인벤토리 슬롯과 가방 창에 표시할 Sprite입니다.

`Item Data`는 여러 씬에서 같은 아이템 정보를 재사용하고 싶을 때만 연결하면 됩니다.
씬의 `Pickup Items`에 직접 입력한 `Id`, `Display Name`, `Description`, `Icon` 값이 있으면 그 값이 우선 사용됩니다.
