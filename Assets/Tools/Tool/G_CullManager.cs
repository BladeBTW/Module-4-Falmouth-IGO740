using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class G_CullManager : MonoBehaviour
{
    public static G_CullManager Instance;
    public List<G_Culler> _Cullers;

    private CullingGroup _cullingGroup;

    private BoundingSphere[] _boundingSphere;
    public float CullingRadius = 10f;
    public float ParticleCullingRadius = 5f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _Cullers = new List<G_Culler>(GetComponentsInChildren<G_Culler>());

        // ⚠️ WebGL safety: only setup if camera exists
        if (Camera.main != null)
        {
            SetupCullingGroup();
        }
        else
        {
            Debug.LogWarning("No Main Camera found for CullingGroup.");
        }
    }

    private void SetupCullingGroup()
    {
        if (_Cullers == null || _Cullers.Count == 0)
            return;

        _cullingGroup = new CullingGroup();

        _cullingGroup.targetCamera = Camera.main;

        _boundingSphere = new BoundingSphere[_Cullers.Count];

        for (int i = 0; i < _Cullers.Count; i++)
        {
            if (_Cullers[i] == null) continue;

            float radius = (_Cullers[i].Type == G_Culler.CullerType.MeshRenderer)
                ? CullingRadius
                : ParticleCullingRadius;

            _boundingSphere[i] = new BoundingSphere(_Cullers[i].Center, radius);

            _Cullers[i].Cull(false);
        }

        _cullingGroup.SetBoundingSpheres(_boundingSphere);
        _cullingGroup.SetBoundingSphereCount(_boundingSphere.Length);
        _cullingGroup.onStateChanged += StateChangedMethod;
    }

    private void StateChangedMethod(CullingGroupEvent evt)
    {
        if (_Cullers == null || evt.index >= _Cullers.Count) return;
        if (_Cullers[evt.index] == null) return;

        _Cullers[evt.index].Cull(evt.isVisible);
    }

    public void AddCuller(G_Culler culler)
    {
        if (_Cullers == null)
            _Cullers = new List<G_Culler>();

        if (!_Cullers.Contains(culler))
            _Cullers.Add(culler);
    }

    private void OnDestroy()
    {
        // ✅ FIX: Null check before Dispose
        if (_cullingGroup != null)
        {
            _cullingGroup.onStateChanged -= StateChangedMethod;
            _cullingGroup.Dispose();
            _cullingGroup = null;
        }
    }
}