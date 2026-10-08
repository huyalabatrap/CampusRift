using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CampusRift.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PanelTransition : MonoBehaviour
    {
        public float Duration = .22f;
        public Selectable FirstSelection;
        CanvasGroup group;
        Coroutine fade;
        public void Show(bool show, bool instant = false)
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (fade != null) StopCoroutine(fade);
            group.interactable = show; group.blocksRaycasts = show;
            if (show) gameObject.SetActive(true);
            if (show && FirstSelection != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(FirstSelection.gameObject);
            if (instant || !gameObject.activeInHierarchy) { group.alpha = show ? 1 : 0; gameObject.SetActive(show); return; }
            fade = StartCoroutine(Fade(show));
        }
        IEnumerator Fade(bool show)
        {
            float from = group.alpha;
            for (float t=0;t<Duration;t+=Time.unscaledDeltaTime)
            { group.alpha = Mathf.Lerp(from,show?1:0,Mathf.SmoothStep(0,1,t/Duration)); yield return null; }
            group.alpha = show ? 1 : 0; fade = null;
            if (!show) gameObject.SetActive(false);
        }
    }
}
