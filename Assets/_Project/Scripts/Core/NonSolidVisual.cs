using UnityEngine;

namespace TheFusionEngineer.Core
{
    /// <summary>
    /// 플레이어와 충돌하지 않아야 하는 순수 시각 연출 계층을 표시합니다.
    /// 이 컴포넌트를 루트 오브젝트에 붙이면 모든 자식 Renderer가 자동 충돌체 생성에서 제외됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NonSolidVisual : MonoBehaviour
    {
        // 이 컴포넌트는 데이터 표식만 담당하므로 실행 로직이 필요하지 않습니다.
    }
}
