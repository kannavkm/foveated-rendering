using UnityEngine;

public class Main : MonoBehaviour
{
    public RenderTexture texturePass0;
    public RenderTexture texturePass1;
    public RenderTexture texturePass2;
    public RenderTexture textureDenoise;

    public Material pass1Material;
    public Material pass2Material;
    public Material denoiseMaterial;

    const float EyeStep = 0.05f;
    const int Width = 1024;
    const int Height = 1024;

    private float timeA;

    private float _sigma;
    private float _alpha;
    private float _eyeX;
    private float _eyeY;
    private int _iApplyLogMap1;
    private int _iApplyLogMap2;

    private int _fps;
    private int _lastfps;
    
    public GameObject dispcam;
    public GameObject dispcam2;

    private bool _isFoveataed;
    
    private ZMQClient _zmqClient;
    private static readonly int IResolutionX = Shader.PropertyToID("_iResolutionX");
    private static readonly int IResolutionY = Shader.PropertyToID("_iResolutionY");
    private static readonly int EyeX = Shader.PropertyToID("_eyeX");
    private static readonly int EyeY = Shader.PropertyToID("_eyeY");
    private static readonly int ScaleRatio = Shader.PropertyToID("_scaleRatio");
    private static readonly int Kernel = Shader.PropertyToID("_kernel");
    private static readonly int IApplyLogMap1 = Shader.PropertyToID("_iApplyLogMap1");
    private static readonly int IApplyLogMap2 = Shader.PropertyToID("_iApplyLogMap2");
    private static readonly int LogTex = Shader.PropertyToID("_LogTex");
    private static readonly int Pass2Tex = Shader.PropertyToID("_Pass2Tex");

    // Use this for initialization
    void Start()
    {
        Debug.Log("Starting");
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 400;
        _sigma = 1.6f;
        _alpha = 4.0f;
        _eyeX = 0.5f;
        _eyeY = 0.5f;
        _iApplyLogMap1 = 1;
        _iApplyLogMap2 = 1;
        _isFoveataed = true;
        dispcam.SetActive(_isFoveataed);
        dispcam2.SetActive(!_isFoveataed);
        timeA = Time.timeSinceLevelLoad;
        DontDestroyOnLoad(this);
        _zmqClient = new ZMQClient();
        _zmqClient.Start();
    }

    // Update is called once per frame
    void Update()
    {
        CalculateFPS();
        _eyeX = _zmqClient._x;
        _eyeY = _zmqClient._y;
        Pass1Main();
        Pass2Main();
        keyControl();
    }

    private void OnDestroy()
    {
        _zmqClient.Stop();
    }

    void CalculateFPS()
    {
        if (Time.timeSinceLevelLoad - timeA <= 1)
        {
            _fps++;
        }
        else
        {
            _lastfps = _fps + 1;
            timeA = Time.timeSinceLevelLoad;
            _fps = 0;
        }
    }

    void Pass1Main()
    {
        pass1Material.SetFloat(IResolutionX, Width);
        pass1Material.SetFloat(IResolutionY, Height);
        pass1Material.SetFloat(EyeX, _eyeX);
        pass1Material.SetFloat(EyeY, _eyeY);
        pass1Material.SetFloat(ScaleRatio, _sigma);
        pass1Material.SetFloat(Kernel, _alpha);
        pass1Material.SetInt(IApplyLogMap1, _iApplyLogMap1);
        Graphics.Blit(texturePass0, texturePass1, pass1Material);
    }

    void Pass2Main()
    {
        pass2Material.SetFloat(IResolutionX, Width);
        pass2Material.SetFloat(IResolutionY, Height);
        pass2Material.SetFloat(EyeX, _eyeX);
        pass2Material.SetFloat(EyeY, _eyeY);
        pass2Material.SetFloat(ScaleRatio, _sigma);
        pass2Material.SetFloat(Kernel, _alpha);
        pass2Material.SetInt(IApplyLogMap2, _iApplyLogMap2);
        pass2Material.SetTexture(LogTex, texturePass1);
        Graphics.Blit(texturePass1, texturePass2, pass2Material);
    }

    void Pass2Denoise()
    {
        denoiseMaterial.SetFloat(IResolutionX, Width);
        denoiseMaterial.SetFloat(IResolutionY, Height);
        denoiseMaterial.SetFloat(EyeX, _eyeX);
        denoiseMaterial.SetFloat(EyeY, _eyeY);
        denoiseMaterial.SetTexture(Pass2Tex, texturePass2);
        Graphics.Blit(texturePass2, textureDenoise, denoiseMaterial);
    }

    void keyControl()
    {
        if (Input.GetKeyDown(KeyCode.D))
            _sigma = _sigma >= 2.6f ? 2.6f : _sigma + 0.2f;
        if (Input.GetKeyDown(KeyCode.A))
            _sigma = _sigma <= 1.0f ? 1.0f : _sigma - 0.2f;
        if (Input.GetKeyDown(KeyCode.W))
            _alpha = _alpha >= 4.0f ? 4.0f : _alpha + 0.2f;
        if (Input.GetKeyDown(KeyCode.S))
            _alpha = _alpha <= 1.0f ? 1.0f : _alpha - 0.2f;
        if (Input.GetKeyDown(KeyCode.F1))
            _iApplyLogMap1 = 1 - _iApplyLogMap1;
        if (Input.GetKeyDown(KeyCode.F2))
            _iApplyLogMap2 = 1 - _iApplyLogMap2;
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _isFoveataed = !_isFoveataed;
            dispcam.SetActive(_isFoveataed);
            dispcam2.SetActive(!_isFoveataed);
        }
    }

    void DispText(int idx, float variable, string name)
    {
        string text = string.Format(name + " = {0}", variable);
        GUI.Label(new Rect(0, idx * 20, Screen.width, Screen.height), text);
    }

    void OnGUI()
    {
        int idx = 0;
        DispText(idx++, _alpha, "alpha");
        DispText(idx++, _sigma, "sigma");
        DispText(idx++, _eyeX, "eyeX");
        DispText(idx++, _eyeY, "eyeY");
        DispText(idx, _lastfps, "FPS:");
    }
}