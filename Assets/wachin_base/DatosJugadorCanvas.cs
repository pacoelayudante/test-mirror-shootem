using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class DatosJugadorCanvas : MonoBehaviour
{
    WachinJugador _jugadorLocal;
    WachinJugador JugadorLocal => _jugadorLocal ? _jugadorLocal : _jugadorLocal = FindObjectOfType<WachinJugador>();

    RectTransform RectTransform => (RectTransform)transform;

    [SerializeField] RectTransform vidaTemplate;
    [SerializeField] RectTransform reloadFill;

    List<System.Action<bool>> vidaSetters = new List<System.Action<bool>>();
    System.Action<float> reloadSetter = null;

    void Start()
    {
        var lastImgVida = vidaTemplate.GetComponentsInChildren<Image>().LastOrDefault();
        vidaSetters.Add(activa => lastImgVida.enabled = activa);

        var imgs = reloadFill.GetComponentsInChildren<Image>();
        var lastImgReload = imgs.LastOrDefault(img => img.type == Image.Type.Filled);
        reloadSetter = (cant) =>
        {
            foreach (var img in imgs) img.enabled = cant < 1f;
            lastImgReload.fillAmount = cant;
        };
    }

    void LateUpdate()
    {
        if (!JugadorLocal)
        {
            foreach (var vida in vidaSetters) vida.Invoke(false);
            reloadSetter.Invoke(1f);
            return;
        }
        transform.position = Camera.main.WorldToScreenPoint(JugadorLocal.transform.position);

        if (vidaSetters.Count < JugadorLocal.maxHp)
        {
            var indicadorVida = Instantiate(vidaTemplate, vidaTemplate.parent);
            var img = indicadorVida.GetComponentsInChildren<Image>().LastOrDefault();
            vidaSetters.Add(activo => img.enabled = activo);
        }

        for (int i = 0; i < vidaSetters.Count; i++)
        {
            vidaSetters[i].Invoke(i < JugadorLocal.HPActual);
        }

        if (JugadorLocal.IsReloading)
        {
            reloadSetter.Invoke(JugadorLocal.ReloadingProgress);
        }
        else
        {
            reloadSetter.Invoke(1f);
        }
    }
}
