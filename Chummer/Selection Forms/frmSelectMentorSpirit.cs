/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.XPath;

namespace Chummer
{
    public partial class frmSelectMentorSpirit : Form
    {
        private bool _blnSkipRefresh = true;
        private string _strForceMentor = string.Empty;

        private readonly XPathNavigator _xmlBaseMentorSpiritDataNode;
        private readonly Character _objCharacter;

        #region Control Events

        public frmSelectMentorSpirit(Character objCharacter, string strXmlFile = "mentors.xml")
        {
            InitializeComponent();

            _objCharacter = objCharacter ?? throw new ArgumentNullException(nameof(objCharacter));
            // Load the Mentor information.
            _xmlBaseMentorSpiritDataNode = objCharacter.LoadDataXPath(strXmlFile).SelectSingleNode("/chummer");
            if (strXmlFile == "paragons.xml")
                Tag = "Title_SelectMentorSpirit_Paragon";
            this.UpdateLightDarkMode();
            this.TranslateWinForm();
        }

        private void lstMentor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_blnSkipRefresh)
                return;
            SuspendLayout();
            try
            {
                XPathNavigator objXmlMentor = null;
                if (lstMentor.SelectedIndex >= 0)
                {
                    string strSelectedId = lstMentor.SelectedValue?.ToString();
                    if (!string.IsNullOrEmpty(strSelectedId))
                        objXmlMentor =
                            _xmlBaseMentorSpiritDataNode.SelectSingleNode("mentors/mentor[id = " +
                                                                          strSelectedId.CleanXPath() + "]");
                }

                if (objXmlMentor != null)
                {
                    cboChoice1.BeginUpdate();
                    cboChoice2.BeginUpdate();

                    // If the Mentor offers a choice of bonuses, build the list and let the user select one.
                    XPathNavigator xmlChoices = objXmlMentor.SelectSingleNode("choices");
                    if (xmlChoices != null)
                    {
                        List<ListItem> lstChoice1 = new List<ListItem>();
                        List<ListItem> lstChoice2 = new List<ListItem>();

                        foreach (XPathNavigator objChoice in xmlChoices.Select("choice"))
                        {
                            string strName = objChoice.SelectSingleNode("name")?.Value ?? string.Empty;
                            if ((_objCharacter.AdeptEnabled ||
                                 !strName.StartsWith("Adept:", StringComparison.Ordinal)) &&
                                (_objCharacter.MagicianEnabled ||
                                 !strName.StartsWith("Magician:", StringComparison.Ordinal)))
                            {
                                if (objChoice.SelectSingleNode("@set")?.Value == "2")
                                    lstChoice2.Add(new ListItem(strName,
                                        objChoice.SelectSingleNode("translate")?.Value ?? strName));
                                else
                                    lstChoice1.Add(new ListItem(strName,
                                        objChoice.SelectSingleNode("translate")?.Value ?? strName));
                            }
                        }

                        //If there is only a single option, show it as a label.
                        //If there are more, show the drop down menu
                        if (lstChoice1.Count > 0)
                            cboChoice1.PopulateWithListItems(lstChoice1);
                        cboChoice1.Visible = lstChoice1.Count > 1;
                        lblBonusText1.Visible = lstChoice1.Count == 1;
                        if (lstChoice1.Count == 1)
                            lblBonusText1.Text = lstChoice1[0].Name;
                        if (lstChoice2.Count > 0)
                            cboChoice2.PopulateWithListItems(lstChoice2);
                        cboChoice2.Visible = lstChoice2.Count > 1;
                        lblBonusText2.Visible = lstChoice2.Count == 1;
                        if (lstChoice2.Count == 1)
                            lblBonusText2.Text = lstChoice2[0].Name;
                    }
                    else
                    {
                        cboChoice1.Visible = false;
                        cboChoice2.Visible = false;
                        lblBonusText1.Visible = false;
                        lblBonusText2.Visible = false;
                    }

                    cboChoice1.EndUpdate();
                    cboChoice2.EndUpdate();
                    lblChoice1.Visible = cboChoice1.Visible;
                    lblChoice2.Visible = cboChoice2.Visible;
                    lblBonus1.Visible = lblBonusText1.Visible;
                    lblBonus2.Visible = lblBonusText2.Visible;

                    // Get the information for the selected Mentor.
                    lblAdvantage.Text = objXmlMentor.SelectSingleNode("altadvantage")?.Value ??
                                        objXmlMentor.SelectSingleNode("advantage")?.Value ??
                                        LanguageManager.GetString("String_Unknown");
                    lblAdvantageLabel.Visible = !string.IsNullOrEmpty(lblAdvantage.Text);
                    lblDisadvantage.Text = objXmlMentor.SelectSingleNode("altdisadvantage")?.Value ??
                                           objXmlMentor.SelectSingleNode("disadvantage")?.Value ??
                                           LanguageManager.GetString("String_Unknown");
                    lblDisadvantageLabel.Visible = !string.IsNullOrEmpty(lblDisadvantage.Text);

                    string strSource = objXmlMentor.SelectSingleNode("source")?.Value ??
                                       LanguageManager.GetString("String_Unknown");
                    string strPage = objXmlMentor.SelectSingleNode("altpage")?.Value ??
                                     objXmlMentor.SelectSingleNode("page")?.Value ??
                                     LanguageManager.GetString("String_Unknown");
                    SourceString objSourceString = new SourceString(strSource, strPage, GlobalSettings.Language,
                        GlobalSettings.CultureInfo, _objCharacter);
                    objSourceString.SetControl(lblSource);
                    lblSourceLabel.Visible = !string.IsNullOrEmpty(lblSource.Text);
                    cmdOK.Enabled = true;
                    tlpRight.Visible = true;
                    tlpBottomRight.Visible = true;
                }
                else
                {
                    tlpRight.Visible = false;
                    tlpBottomRight.Visible = false;
                    cmdOK.Enabled = false;
                }
            }
            finally
            {
                ResumeLayout();
            }
        }

        /// <summary>
        /// Accept the selected item and close the form.
        /// </summary>
        private void AcceptForm(object sender, EventArgs e)
        {
            string strSelectedId = lstMentor.SelectedValue?.ToString();
            if (!string.IsNullOrEmpty(strSelectedId))
            {
                XPathNavigator objXmlMentor = _xmlBaseMentorSpiritDataNode.SelectSingleNode("mentors/mentor[id = " + strSelectedId.CleanXPath() + "]");
                if (objXmlMentor == null)
                    return;

                SelectedMentor = strSelectedId;

                DialogResult = DialogResult.OK;
            }
        }

        /// <summary>
        /// Populate the Mentor list.
        /// </summary>
        private void RefreshMentorsList(object sender, EventArgs e)
        {
            string strForceId = string.Empty;

            string strFilter = '(' + _objCharacter.Settings.BookXPath() + ')';
            if (!string.IsNullOrEmpty(txtSearch.Text))
                strFilter += " and " + CommonFunctions.GenerateSearchXPath(txtSearch.Text);
            List<ListItem> lstMentors = new List<ListItem>();
            foreach (XPathNavigator objXmlMentor in _xmlBaseMentorSpiritDataNode.Select("mentors/mentor[" + strFilter + "]"))
            {
                if (!objXmlMentor.RequirementsMet(_objCharacter)) continue;

                string strName = objXmlMentor.SelectSingleNode("name")?.Value ?? LanguageManager.GetString("String_Unknown");
                string strId = objXmlMentor.SelectSingleNode("id")?.Value ?? string.Empty;
                if (strName == _strForceMentor)
                    strForceId = strId;
                lstMentors.Add(new ListItem(strId, objXmlMentor.SelectSingleNode("translate")?.Value ?? strName));
            }
            lstMentors.Sort(CompareListItems.CompareNames);
            string strOldSelected = lstMentor.SelectedValue?.ToString();
            _blnSkipRefresh = true;
            lstMentor.BeginUpdate();
            lstMentor.PopulateWithListItems(lstMentors);
            _blnSkipRefresh = false;
            if (!string.IsNullOrEmpty(strOldSelected))
                lstMentor.SelectedValue = strOldSelected;
            else
                lstMentor.SelectedIndex = -1;
            if (!string.IsNullOrEmpty(strForceId))
            {
                lstMentor.SelectedValue = strForceId;
                lstMentor.Enabled = false;
            }
            lstMentor.EndUpdate();
        }

        private void OpenSourceFromLabel(object sender, EventArgs e)
        {
            CommonFunctions.OpenPdfFromControl(sender, e);
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        #endregion Control Events

        #region Properties

        /// <summary>
        /// Forced selection for mentor spirit
        /// </summary>
        public string ForcedMentor
        {
            set => _strForceMentor = value;
        }

        /// <summary>
        /// Mentor that was selected in the dialogue.
        /// </summary>
        public string SelectedMentor { get; private set; } = string.Empty;

        /// <summary>
        /// First choice that was selected in the dialogue.
        /// </summary>
        public string Choice1 => cboChoice1.SelectedValue?.ToString() ?? string.Empty;

        /// <summary>
        /// Second choice that was selected in the dialogue.
        /// </summary>
        public string Choice2 => cboChoice2.SelectedValue?.ToString() ?? string.Empty;

        #endregion Properties
    }
}
