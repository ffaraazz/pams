<#import "template.ftl" as layout>
<@layout.registrationLayout displayInfo=true displayMessage=!messagesPerField.existsError('username'); section>

<#if section == "header">
  Reset Password

<#elseif section == "form">

<div id="kc-container" class="kc-login-container">

  <img src="https://nexturn.com/wp-content/uploads/2026/01/LOGO_White_WT.png?x19308"
       alt="NexTurn" class="nf-company-logo" />

  <div class="nf-content">

  <div class="nf-brand">
    <div class="nf-app-logo-wrap">
      <svg class="nf-app-icon" viewBox="0 0 44 44" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
        <rect width="44" height="44" rx="10" fill="#5048e5"/>
        <path d="M11 32 L11 12 L19 12 L27 25 L27 12 L33 12 L33 32 L25 32 L17 19 L17 32 Z" fill="white"/>
      </svg>
      <div class="nf-app-text">
        <span class="nf-app-name">NexFlow</span>
        <p class="nf-tagline">Manager Allocation Console</p>
      </div>
    </div>
  </div>

  <div class="nf-card">
    <div class="nf-card-header">
      <h1>${msg("emailForgotTitle")}</h1>
      <p>Enter your email and we'll send you a reset link</p>
    </div>

    <#if message?has_content && (message.type != 'warning' || !isAppInitiatedAction??)>
      <div class="nf-alert nf-alert-${message.type}" role="alert" aria-live="polite">
        <#if message.type == 'error'>
          <svg class="nf-alert-icon" viewBox="0 0 16 16" fill="currentColor"><path d="M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1Zm-.75 4a.75.75 0 0 1 1.5 0v3.5a.75.75 0 0 1-1.5 0V5Zm.75 6.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2Z"/></svg>
        <#elseif message.type == 'success'>
          <svg class="nf-alert-icon" viewBox="0 0 16 16" fill="currentColor"><path d="M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1Zm3.78 5.47-4.5 4.5a.75.75 0 0 1-1.06 0l-2-2a.75.75 0 1 1 1.06-1.06l1.47 1.47 3.97-3.97a.75.75 0 1 1 1.06 1.06Z"/></svg>
        <#else>
          <svg class="nf-alert-icon" viewBox="0 0 16 16" fill="currentColor"><path d="M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1ZM7.25 5a.75.75 0 0 1 1.5 0v3.5a.75.75 0 0 1-1.5 0V5Zm.75 6.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2Z"/></svg>
        </#if>
        <span>${kcSanitize(message.summary)?no_esc}</span>
      </div>
    </#if>

    <form id="kc-reset-form" action="${url.loginAction}" method="post">
      <div class="nf-form-group">
        <label class="nf-label" for="username">
          <#if realm.loginWithEmailAllowed && !realm.registrationEmailAsUsername>
            ${msg("usernameOrEmail")}
          <#else>
            ${msg("email")}
          </#if>
        </label>
        <div class="nf-input-wrap">
          <input id="username" name="username" type="text"
            class="nf-input<#if messagesPerField.existsError('username')> nf-input-error</#if>"
            value="${(auth.attemptedUsername!'')}"
            autocomplete="username" autocapitalize="none" autocorrect="off" spellcheck="false" autofocus
            placeholder="you@company.com"
          />
        </div>
        <#if messagesPerField.existsError('username')>
          <p class="nf-field-error">
            <svg width="12" height="12" viewBox="0 0 16 16" fill="currentColor"><path d="M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1Zm-.75 4a.75.75 0 0 1 1.5 0v3.5a.75.75 0 0 1-1.5 0V5Zm.75 6.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2Z"/></svg>
            ${kcSanitize(messagesPerField.get('username'))?no_esc}
          </p>
        </#if>
      </div>

      <button class="nf-btn" type="submit">
        <span class="nf-btn-spinner" aria-hidden="true"></span>
        <span class="nf-btn-text">${msg("doSubmit")}</span>
      </button>
    </form>

    <div style="margin-top: 1rem; text-align: center;">
      <a class="nf-link" href="${url.loginUrl}">&larr; Back to sign in</a>
    </div>
  </div>

  </div><!-- .nf-content -->

  <div class="nf-footer">
    &copy; ${.now?string('yyyy')} NexFlow &mdash; Powered by NexTurn
  </div>

</div>

<script>
(function () {
  var form = document.getElementById('kc-reset-form');
  var btn = form ? form.querySelector('.nf-btn') : null;
  if (form && btn) {
    form.addEventListener('submit', function () {
      btn.classList.add('nf-loading');
      btn.disabled = true;
    });
  }
}());
</script>

</#if>
</@layout.registrationLayout>