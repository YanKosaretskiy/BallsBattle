using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SwordTrail : MonoBehaviour // повесить на SwordCenter — объект, который вращается
{
    // сслыки на внутренние компоненты
    private Ball _Ball;
    private LineRenderer _LineRenderer;

    // данные об ультимейте
    private float curUltimateCharge;
    private float chargeMaxStats = 75f;

    // данные о следе меча
    private float innerRadius = 0.5f;   // точка начала меча
    private float outerRadius = 1.9f;   // точка кончика меча
    private float rotSpeed    = 1f;     // направление движения(против часовой)
    private int   segments    = 30;     // кол-во точек в дуге следа
    private float maxTrailAngle = 180f; // на сколько градусов "назад" тянется след
    private float curTrailAngle;        // текущий угол
    private float centerRadius;         // радиус центра
    private float bladeLength;          // длина меча


    private void Awake()
    {
        _LineRenderer = GetComponent<LineRenderer>();
        _LineRenderer.useWorldSpace = false; // использовать локальные точки
        _LineRenderer.positionCount = segments;

        _Ball = GetComponentInParent<Ball>();

        // подсчет радиусов
        centerRadius = (innerRadius + outerRadius) / 2f;
        bladeLength = outerRadius - innerRadius;

        _LineRenderer.startWidth = bladeLength;
        _LineRenderer.endWidth = bladeLength;
    }

    private void Update()
    {
        curUltimateCharge = _Ball.curUltimateCharge;
        curTrailAngle = (curUltimateCharge / chargeMaxStats) * maxTrailAngle;

        float dir = Mathf.Sign(rotSpeed);
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            float angle = -dir * t * curTrailAngle * Mathf.Deg2Rad;
            _LineRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * centerRadius);
        }
    }
}
