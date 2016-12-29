﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Fritz.WebFormsTest.Internal;

namespace Fritz.WebFormsTest
{

    public static class PageExtensions
    {

        private static readonly Type _Type = typeof(Page);

        /// <summary>
        /// Render the HTML for this page using the Render method and return the rendered string
        /// </summary>
        /// <returns></returns>
        public static string RenderHtml(this Page myPage)
        {

            var sb = new StringBuilder();
            var txt = new HtmlTextWriter(new StringWriter(sb));

            var renderMethod = _Type.GetMethod("Render", BindingFlags.Instance | BindingFlags.NonPublic);
            renderMethod.Invoke(myPage, new object[] { txt });

            return sb.ToString();

        }

        public static Page SetPageState<T>(this Page myPage, string controlId, Action<T> controlConfig) where T : Control
        {

            // Prevent this method from being called by non-test operations
            if (!WebApplicationProxy.IsInitialized) throw new InvalidOperationException("A WebApplicationProxy is needed to set page state");

            var c = myPage.FindControl(controlId);

            if (c == null) throw new ArgumentException($"Unable to locate the control '{controlId}'");
            if (!(c is T)) throw new ArgumentException($"The control '{controlId}' is not of type '{typeof(T).FullName}'");

            controlConfig(c as T);

            return myPage;

        }

        public static void MockPostData(this Page myPage, NameValueCollection postData)
        {

            // Ensure that the control structure is available
            var escMethod = _Type.GetMethod("EnsureChildControls", BindingFlags.Instance | BindingFlags.NonPublic);
            escMethod.Invoke(myPage, null);

            // Set the local fields that the postData SHOULD have come from
            var requestValueField = _Type.GetField("_requestValueCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            requestValueField.SetValue(myPage, postData);


            var collectionField = _Type.GetField("_unvalidatedRequestValueCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            collectionField.SetValue(myPage, postData);


            // Load the data
            var hiddenMethod = _Type.GetMethod("ProcessPostData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            hiddenMethod.Invoke(myPage, new object[] { postData, true });

        }

        /// <summary>
        /// Allows system under test to simulate that the Page is valid without relying on the Validate method and validators.
        /// Note, using this method mutates the Validators property to achieve the desired effect.
        /// </summary>
        /// <param name="myPage"></param>
        /// <param name="isValid">The value that IsValid property on the page instance should return.</param>
        public static void MockIsValid(this Page myPage, bool isValid)
        {
            var pageType = typeof(Page);

            // Simulates that the page has been validated so that the call to IsValid does not throw an HttpException.
            var validatedField = pageType.GetField("_validated", BindingFlags.NonPublic | BindingFlags.Instance);
            validatedField?.SetValue(myPage, true);

            // Null out the private _validators field that is backing the Validators property.
            var validatorsField = pageType.GetField("_validators", BindingFlags.NonPublic | BindingFlags.Instance);
            validatorsField?.SetValue(myPage, null);

            // With the _validators nulled out, we know that the FakeValidator will be the only validator added to the collection. The read only
            // property creates a new ValidatorCollection when its backing ivar is null.
            myPage.Validators.Add(new FakeValidator(isValid));
        }

        public static void FireEvent(this Page myPage, WebFormEvent e)
        {
            myPage.FireEvent(e, EventArgs.Empty);
        }

        public static void FireEvent(this Page myPage, WebFormEvent e, EventArgs args)
        {

            Dictionary<WebFormEvent, bool> _EventsTriggered = new Dictionary<WebFormEvent, bool>();
            if (myPage.Items.Contains("eventsTriggered"))
                _EventsTriggered = myPage.Items["eventsTriggered"] as Dictionary<WebFormEvent, bool>;

            if (_EventsTriggered.ContainsKey(e))
            {
                throw new InvalidOperationException($"Previously triggered the {e.ToString()} event");
            }

            _EventsTriggered.Add(e, true);
            myPage.Items["eventsTriggered"] = _EventsTriggered;

            string methodName = "";

            switch (e)
            {
                case WebFormEvent.Init:
                    //methodName = "OnInit";
                    break;
                case WebFormEvent.Load:
                    myPage.LoadRecursiveInternal();
                    break;
                case WebFormEvent.PreRender:
                    //methodName = "OnPreRender";
                    myPage.PreRenderRecursiveInternal();
                    break;
                case WebFormEvent.Unload:
                    methodName = "OnUnload";
                    break;
                default:
                    break;
            }

            if (methodName == string.Empty) return;

            var thisMethod = _Type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            thisMethod.Invoke(myPage, new object[] { args });

        }

        public static void InvokeClick<T>(this Page page, string controlName, EventArgs args) where T : class, IButtonControl, new()
        {
            var button = page.FindControlRecurse(controlName) as T;
            var clickMethod = typeof(T).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance);

            clickMethod.Invoke(button, new object[] { args });
        }

        public static void RunToEvent(this Page myPage, WebFormEvent evt = WebFormEvent.None)
        {

            myPage.FireEvent(WebFormEvent.Init);
            if (evt == WebFormEvent.Init) return;

            myPage.FireEvent(WebFormEvent.Load);
            if (evt == WebFormEvent.Load) return;

            myPage.FireEvent(WebFormEvent.PreRender);
            if (evt == WebFormEvent.PreRender) return;

            myPage.FireEvent(WebFormEvent.Unload);

        }

        /// <summary>
        /// Helper property for inherited pages to indicate that we are in 'unit test mode'
        /// </summary>
        public static bool IsInTestMode(this Page myPage)
        {

            return (HttpContext.Current == null ||
              (HttpContext.Current.Items.Contains("IsInTestMode") && (bool)(HttpContext.Current.Items["IsInTestMode"])));

        }


        public static T FindControl<T>(this Page myPage, string controlId) where T : Control
        {

            return myPage.FindControl(controlId) as T;

        }

        internal static void PreRenderRecursiveInternal(this Page myPage)
        {

            var thisMethod = _Type.GetMethod("PreRenderRecursiveInternal", BindingFlags.Instance | BindingFlags.NonPublic);
            thisMethod.Invoke(myPage, new object[] { });

        }

        internal static void LoadRecursiveInternal(this Page myPage)
        {

            var thisMethod = _Type.GetMethod("LoadRecursive", BindingFlags.Instance | BindingFlags.NonPublic);
            thisMethod.Invoke(myPage, new object[] { });

        }

    }

}
