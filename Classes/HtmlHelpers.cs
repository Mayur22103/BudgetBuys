using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Project.Classes
{
    public static class HtmlHelpers
    {
        public static MvcHtmlString UserTypeRadioButton(
            this HtmlHelper htmlHelper,
            string name,
            string value,
            string labelText,
            bool isChecked = false)
        {
            // Create the radio button
            var radioButton = htmlHelper.RadioButton(name, value, isChecked, new { @class = "form-check-input", onclick = "toggleForm()" });
            // Create the label
            var label = htmlHelper.Label(name, labelText, new { @class = "form-check-label", @for = name });

            // Combine radio button and label into a div
            return MvcHtmlString.Create($"<div class='form-check'>{radioButton} {label}</div>");
        }
    }
}