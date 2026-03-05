<#import "template.ftl" as layout>
    <@layout.registrationLayout displayMessage=!messagesPerField.existsError('username','password') displayInfo=realm.password && realm.registrationAllowed && !registrationDisabled??; section>
        <#if section=="header">
            Sign In
            <#elseif section=="form">
                <div id="kc-container" class="kc-login-container">
                    <!-- Company Logo (Nexturn) - fixed top-right of screen -->
                    <img src="https://nexturn.com/wp-content/uploads/2026/01/LOGO_White_WT.png?x19308"
                        alt="NexTurn" class="nf-company-logo" />
                    <div class="nf-content">
                        <!-- Brand -->
                        <div class="nf-brand">
                            <!-- App Logo (NexFlow) -->
                            <div class="nf-app-logo-wrap">
                                <svg class="nf-app-icon" viewBox="0 0 44 44" xmlns="http://www.w3.org/2000/svg">
                                    <defs>
                                        <!-- Shine gradient -->
                                        <linearGradient id="shine" x1="0" y1="0" x2="1" y2="1">
                                            <stop offset="0%" stop-color="white" stop-opacity="0" />
                                            <stop offset="50%" stop-color="white" stop-opacity="0.7" />
                                            <stop offset="100%" stop-color="white" stop-opacity="0" />
                                        </linearGradient>
                                        <!-- Clip everything to the rounded square -->
                                        <clipPath id="clipLogo">
                                            <rect width="44" height="44" rx="10" />
                                        </clipPath>
                                    </defs>
                                    <!-- Logo Background -->
                                    <rect width="44" height="44" rx="10" fill="#5048e5" />
                                    <!-- Logo Letter -->
                                    <path d="M11 32 L11 12 L19 12 L27 25 L27 12 L33 12 L33 32 L25 32 L17 19 L17 32 Z" fill="white" />
                                    <!-- Shine -->
                                    <g clip-path="url(#clipLogo)">
                                        <rect class="shine" x="-60" y="0" width="40" height="80" fill="url(#shine)" transform="rotate(25)" />
                                    </g>
                                </svg>
                                <div class="nf-app-text">
                                    <span class="nf-app-name">NexFlow</span>
                                    <p class="nf-tagline">Enterprise Resource Planning</p>
                                </div>
                            </div>
                        </div>
                        <!-- Form Area (no card box) -->
                        <div class="nf-card">
                            <div class="nf-card-header">
                                <h1>Welcome Back</h1>
                                <p>Sign in to continue</p>
                            </div>
                            <!-- Alert -->
                            <#if message?has_content && (message.type !='warning' || !isAppInitiatedAction??)>
                                <div class="nf-alert nf-alert-${message.type}" role="alert" aria-live="polite">
                                    <#if message.type=='error'>
                                        <svg class="nf-alert-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                                            <path d="M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1Zm-.75 4a.75.75 0 0 1 1.5 0v3.5a.75.75 0 0 1-1.5 0V5Zm.75 6.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2Z" />
                                        </svg>
                                        <#elseif message.type=='success'>
                                            <svg class="nf-alert-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                                                <path d="M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1Zm3.78 5.47-4.5 4.5a.75.75 0 0 1-1.06 0l-2-2a.75.75 0 1 1 1.06-1.06l1.47 1.47 3.97-3.97a.75.75 0 1 1 1.06 1.06Z" />
                                            </svg>
                                            <#else>
                                                <svg class="nf-alert-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                                                    <path d="M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1ZM7.25 5a.75.75 0 0 1 1.5 0v3.5a.75.75 0 0 1-1.5 0V5Zm.75 6.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2Z" />
                                                </svg>
                                    </#if>
                                    <span>
                                        ${kcSanitize(message.summary)?no_esc}
                                    </span>
                                </div>
                            </#if>
                            <!-- Login Form -->
                            <form id="kc-form-login" action="${url.loginAction}" method="post">
                                <div class="nf-form-group">
                                    <label class="nf-label" for="username">
                                        <#if !realm.loginWithEmailAllowed>
                                            ${msg("username")}
                                            <#elseif !realm.registrationEmailAsUsername>
                                                ${msg("usernameOrEmail")}
                                                <#else>
                                                    ${msg("email")}
                                        </#if>
                                    </label>
                                    <div class="nf-input-wrap">
                                        <input id="username" name="username" type="text"
                                            class="nf-input<#if messagesPerField.existsError('username','password')> nf-input-error</#if>"
                                            value="${(login.username!'')}"
                                            autocomplete="username" autocapitalize="none" autocorrect="off" spellcheck="false" autofocus
                                            placeholder="<#if !realm.loginWithEmailAllowed>your username<#elseif !realm.registrationEmailAsUsername>username or email<#else>you@company.com</#if>" />
                                    </div>
                                    <#if messagesPerField.existsError('username') && !messagesPerField.existsError('password')>
                                        <p class="nf-field-error">
                                            <svg width="12" height="12" viewBox="0 0 16 16" fill="currentColor">
                                                <path d="M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1Zm-.75 4a.75.75 0 0 1 1.5 0v3.5a.75.75 0 0 1-1.5 0V5Zm.75 6.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2Z" />
                                            </svg>
                                            ${kcSanitize(messagesPerField.get('username'))?no_esc}
                                        </p>
                                    </#if>
                                </div>
                                <div class="nf-form-group">
                                    <label class="nf-label" for="password">
                                        ${msg("password")}
                                    </label>
                                    <div class="nf-input-wrap">
                                        <input id="password" name="password" type="password"
                                            class="nf-input nf-input-icon-right<#if messagesPerField.existsError('username','password')> nf-input-error</#if>"
                                            autocomplete="current-password" placeholder="••••••••" />
                                        <button type="button" class="nf-input-toggle" id="password-toggle" aria-label="Toggle password visibility" aria-pressed="false">
                                            <svg id="eye-show" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5">
                                                <path d="M2 10s3-6 8-6 8 6 8 6-3 6-8 6-8-6-8-6Z" stroke-linecap="round" stroke-linejoin="round" />
                                                <circle cx="10" cy="10" r="2.5" stroke-linecap="round" stroke-linejoin="round" />
                                            </svg>
                                            <svg id="eye-hide" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.5" style="display:none">
                                                <path d="M3 3l14 14M7.5 7.6A4 4 0 0 0 10 14a4 4 0 0 0 3.9-3M4.5 4.7C3 6 2 8 2 10s3 6 8 6c1.8 0 3.4-.5 4.7-1.4M17.5 15.4C19 14 20 12 20 10s-3-6-8-6c-.8 0-1.6.1-2.3.3" stroke-linecap="round" stroke-linejoin="round" />
                                            </svg>
                                        </button>
                                    </div>
                                    <#if messagesPerField.existsError('password')>
                                        <p class="nf-field-error">
                                            <svg width="12" height="12" viewBox="0 0 16 16" fill="currentColor">
                                                <path d="M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1Zm-.75 4a.75.75 0 0 1 1.5 0v3.5a.75.75 0 0 1-1.5 0V5Zm.75 6.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2Z" />
                                            </svg>
                                            ${kcSanitize(messagesPerField.get('password'))?no_esc}
                                        </p>
                                    </#if>
                                </div>
                                <#if realm.rememberMe?? && realm.rememberMe || realm.resetPasswordAllowed>
                                    <div class="nf-row">
                                        <#if realm.rememberMe?? && realm.rememberMe>
                                            <label class="nf-check-group">
                                                <input class="nf-check" id="rememberMe" name="rememberMe" type="checkbox"
                                                    <#if login.rememberMe?? && login.rememberMe>checked
                                        </#if> />
                                        <span class="nf-check-label">
                                            ${msg("rememberMe")}
                                        </span>
                                        </label>
                                        <#else><span></span>
                                </#if>
                                <#if realm.resetPasswordAllowed>
                                    <a class="nf-link" href="${url.loginResetCredentialsUrl}">
                                        ${msg("doForgotPassword")}
                                    </a>
                                </#if>
                        </div>
        </#if>
        <input type="hidden" id="id-hidden-input" name="credentialId"
            <#if auth.selectedCredential?has_content>value="${auth.selectedCredential}"</#if> />
        <button id="kc-login" class="nf-btn" type="submit" name="login">
            <span class="nf-btn-spinner" aria-hidden="true"></span>
            <span class="nf-btn-text">Sign In</span>
        </button>
        </form>
        </div>
        </div><!-- .nf-content -->
        <div class="nf-footer">
            &copy; ${.now?string('yyyy')} NexFlow &mdash; Powered by NexTurn
        </div>
        </div>
        <script>
        (function() {
            // Password toggle
            var btn = document.getElementById('password-toggle');
            var field = document.getElementById('password');
            var iconShow = document.getElementById('eye-show');
            var iconHide = document.getElementById('eye-hide');
            if (btn && field) {
                btn.addEventListener('click', function() {
                    var isPassword = field.type === 'password';
                    field.type = isPassword ? 'text' : 'password';
                    iconShow.style.display = isPassword ? 'none' : '';
                    iconHide.style.display = isPassword ? '' : 'none';
                    btn.setAttribute('aria-pressed', String(isPassword));
                    field.focus();
                });
            }
            // Loading on submit
            var form = document.getElementById('kc-form-login');
            var submitBtn = document.getElementById('kc-login');
            if (form && submitBtn) {
                form.addEventListener('submit', function() {
                    submitBtn.classList.add('nf-loading');
                    submitBtn.disabled = true;
                });
            }
        }());
        </script>
        </#if>
    </@layout.registrationLayout>