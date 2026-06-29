using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AIStatusUIView : UIView
{
    [Header("UI Refs")]
    public TMP_Text cashPriceText;
    public TMP_Text nameText;
    public Image healthFillImage;

    [Header("Target")]
    public Creature targetCreature;

    [Header("Display")]
    public float visibleSeconds = 3f;

    [Header("Mobile Optimization")]
    public bool updateOnlyWhileVisible = true;
    public float maxShowDistance = 60f;
    public bool flipByCameraSide = true;

    private const float HealthToFill = 0.01f; // health / 100f

    private Coroutine _hideRoutine;
    private Camera _mainCam;
    private bool _isVisibleRuntime;

    private Vector3 _initialLocalScale;
    private float _maxShowDistanceSqr;
    private float _lastHealthFill = -1f;
    private int _lastCashPrice = int.MinValue;
    private float _lastScaleX;

    public override void Init()
    {
        base.Init();

        _mainCam = Camera.main;

        if (targetCreature == null)
            targetCreature = GetComponentInParent<Creature>();

        _initialLocalScale = transform.localScale;
        _lastScaleX = _initialLocalScale.x;
        _maxShowDistanceSqr = maxShowDistance * maxShowDistance;

        RefreshNow();
    }

    private void LateUpdate()
    {
        if (targetCreature == null)
            return;

        if (!_isVisibleRuntime && updateOnlyWhileVisible)
            return;

        if (flipByCameraSide)
            UpdateFlipByCameraSide();

        if (_isVisibleRuntime)
            RefreshHealthOnly();
    }

    public void Bind(Creature creature)
    {
        targetCreature = creature;
        RefreshNow();
    }

    public void RefreshNow()
    {
        if (targetCreature == null)
            return;

        // Agar naam dikhana ho to uncomment kar lo:
        // if (nameText != null)
        //     nameText.text = targetCreature.specie;

        if (cashPriceText != null)
        {
            int currentCashPrice = targetCreature.cashPrice;
            if (_lastCashPrice != currentCashPrice)
            {
                _lastCashPrice = currentCashPrice;
                cashPriceText.text = currentCashPrice.ToString();
            }
        }

        RefreshHealthOnly();
    }

    private void RefreshHealthOnly()
    {
        if (healthFillImage == null || targetCreature == null)
            return;

        float fill = Mathf.Clamp01(targetCreature.health * HealthToFill);

        if (!Mathf.Approximately(_lastHealthFill, fill))
        {
            _lastHealthFill = fill;
            healthFillImage.fillAmount = fill;
        }
    }

    private void UpdateFlipByCameraSide()
    {
        if (_mainCam == null)
            return;

        Transform reference = targetCreature != null ? targetCreature.transform : transform;
        Vector3 dirToCam = _mainCam.transform.position - reference.position;

        float dot = Vector3.Dot(reference.right, dirToCam);
        float desiredX = dot > 0f ? -Mathf.Abs(_initialLocalScale.x) : Mathf.Abs(_initialLocalScale.x);

        if (!Mathf.Approximately(_lastScaleX, desiredX))
        {
            _lastScaleX = desiredX;

            Vector3 scale = transform.localScale;
            scale.x = desiredX;
            transform.localScale = scale;
        }
    }

    public void ShowForSeconds()
    {
        if (targetCreature == null)
            return;

        if (_mainCam == null)
            _mainCam = Camera.main;

        if (_mainCam != null)
        {
            Vector3 offset = _mainCam.transform.position - targetCreature.transform.position;
            if (offset.sqrMagnitude > _maxShowDistanceSqr)
                return;
        }

        RefreshNow();
        Show();

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        _hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(visibleSeconds);
        Hide();
        _hideRoutine = null;
    }

    public override void Show()
    {
        base.Show();
        _isVisibleRuntime = true;

        if (flipByCameraSide)
            UpdateFlipByCameraSide();
    }

    public override void Hide()
    {
        base.Hide();
        _isVisibleRuntime = false;
    }

    private void OnDisable()
    {
        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        _isVisibleRuntime = false;
    }
}

//using System.Collections;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class AIStatusUIView : UIView
//{
//    [Header("Target")]
//    public Creature targetCreature;

//    [Header("UI Refs")]
//    public TMP_Text nameText;
//    public Image healthFillImage;

//    [Header("Display")]
//    public float visibleSeconds = 3f;
//    public bool billboardToMainCamera = true;
//    public Vector3 worldOffset = new Vector3(0f, 2.5f, 0f);

//    [Header("Mobile Optimization")]
//    public bool updateOnlyWhileVisible = true;
//    public float maxShowDistance = 60f;

//    private Coroutine _hideRoutine;
//    private Camera _mainCam;
//    private bool _isVisibleRuntime;

//    public override void Init()
//    {
//        base.Init();
//        _mainCam = Camera.main;

//        if (targetCreature == null)
//            targetCreature = GetComponentInParent<Creature>();

//        RefreshNow();
//    }

//    private void LateUpdate()
//    {
//        if (!_isVisibleRuntime && updateOnlyWhileVisible)
//            return;

//        if (targetCreature == null)
//            return;

//        if (billboardToMainCamera)
//        {
//            if (_mainCam == null)
//                _mainCam = Camera.main;

//            if (_mainCam != null)
//            {
//                Vector3 camForward = _mainCam.transform.forward;
//                transform.forward = camForward;
//            }
//        }

//        Transform root = targetCreature.transform;
//        transform.position = root.position + worldOffset;

//        if (_isVisibleRuntime)
//            RefreshHealthOnly();
//    }

//    public void Bind(Creature creature)
//    {
//        targetCreature = creature;
//        RefreshNow();
//    }

//    public void RefreshNow()
//    {
//        if (targetCreature == null)
//            return;

//        if (nameText != null)
//            nameText.text = targetCreature.specie;

//        RefreshHealthOnly();
//    }

//    private void RefreshHealthOnly()
//    {
//        if (targetCreature == null)
//            return;

//        if (healthFillImage != null)
//            healthFillImage.fillAmount = Mathf.Clamp01(targetCreature.health / 100f);
//    }

//    public void ShowForSeconds()
//    {
//        if (targetCreature == null)
//            return;

//        if (_mainCam == null)
//            _mainCam = Camera.main;

//        if (_mainCam != null)
//        {
//            float dist = Vector3.Distance(_mainCam.transform.position, targetCreature.transform.position);
//            if (dist > maxShowDistance)
//                return;
//        }

//        RefreshNow();
//        Show();
//        _isVisibleRuntime = true;

//        if (_hideRoutine != null)
//            StopCoroutine(_hideRoutine);

//        _hideRoutine = StartCoroutine(HideAfterDelay());
//        Debug.Log("ShowForSeconds called on " + gameObject.name);
//    }

//    private IEnumerator HideAfterDelay()
//    {
//        yield return new WaitForSeconds(visibleSeconds);
//        Hide();
//        _isVisibleRuntime = false;
//        _hideRoutine = null;
//    }

//    public override void Hide()
//    {
//        base.Hide();
//        _isVisibleRuntime = false;
//    }

//    public override void Show()
//    {
//        base.Show();
//        _isVisibleRuntime = true;
//    }
//}