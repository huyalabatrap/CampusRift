using TMPro;
using UnityEngine;
namespace CampusRift.UI
{
    public sealed class ObjectiveUI : MonoBehaviour
    {
        public TMP_Text Body, Status;
        public CampusRiftUITheme Theme;
        public void SetObjective(string text) {Body.text=text;Status.text="OBJECTIVE";Status.color=Theme.TextSecondary;}
        public void CompleteObjective() {Status.text="[ COMPLETE ]";Status.color=Theme.SuccessColor;}
    }
}
