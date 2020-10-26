using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class MiraJugadorCanvas : MonoBehaviour
{
    WachinJugador _jugadorLocal;
    // WachinJugador JugadorLocal => _jugadorLocal ? _jugadorLocal : _jugadorLocal = FindObjectOfType<WachinJugador>();
    WachinJugador JugadorLocal => WachinJugador.local;

    RectTransform RectTransform => (RectTransform)transform;

    [SerializeField] Sprite balaCargada, balaDescargada;

    [SerializeField] RectTransform balaTemplate, vidaTemplate;

    List<System.Action<Sprite, bool>> balaSetters = new List<System.Action<Sprite, bool>>();
    List<System.Action<bool>> vidaSetters = new List<System.Action<bool>>();

    private void OnEnable()
    {
        Cursor.visible = false;
    }
    private void OnDisable()
    {
        Cursor.visible = true;
    }

    void Start()
    {
        var imgs = balaTemplate.GetComponentsInChildren<Image>();
        balaSetters.Add((sprite, activo) =>
        {
            foreach (var i in imgs)
            {
                i.enabled = activo;
                i.sprite = sprite;
            }
        });

        var lastImg = vidaTemplate.GetComponentsInChildren<Image>().LastOrDefault();
        vidaSetters.Add(activa => lastImg.enabled = activa);
    }

    void LateUpdate()
    {
        if (!JugadorLocal)
        {
            foreach(var bala in balaSetters) bala.Invoke(balaDescargada, false);
            foreach(var vida in vidaSetters) vida.Invoke(false);
            transform.position = Input.mousePosition;
            return;
        }
        transform.position = Input.mousePosition;

        if (balaSetters.Count < JugadorLocal.ClipSize)
        {
            var indicadorBala = Instantiate(balaTemplate, balaTemplate.parent);
            var imgs = indicadorBala.GetComponentsInChildren<Image>();
            balaSetters.Add((sprite, activo) =>
            {
                foreach (var i in imgs)
                {
                    i.enabled = activo;
                    i.sprite = sprite;
                }
            });
        }
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
            var reloadedCount = JugadorLocal.ReloadingProgress * JugadorLocal.ClipSize;
            for (int i = 0; i < balaSetters.Count; i++)
            {
                balaSetters[i].Invoke(balaDescargada, i > reloadedCount);
            }
        }
        else
        {
            for (int i = 0; i < balaSetters.Count; i++)
            {
                balaSetters[i].Invoke(i < JugadorLocal.CurrentBulletCount ? balaCargada : balaDescargada, true);
            }
        }
    }
}
