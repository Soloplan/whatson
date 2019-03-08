namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System.Xml;

  public interface IStatusGuiProvider
  {
    SubjectViewModel GetViewModel();
    XmlReader GetDataTempletXaml();
  }
}