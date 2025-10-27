# # module.auth0.auth0_client.web:
# resource "auth0_client" "web" {
#     allowed_clients                                      = []
#     allowed_logout_urls                                  = [
#         "https://mycrocloud.info",
#     ]
#     allowed_origins                                      = []
#     app_type                                             = "spa"
#     callbacks                                            = [
#         "https://mycrocloud.info",
#     ]
#     client_aliases                                       = []
#     client_id                                            = "8YECKUMM3I7ejQgsVky40b5LooQB0YFf"
#     client_metadata                                      = {}
#     compliance_level                                     = [90mnull[0m[0m
#     cross_origin_auth                                    = true
#     cross_origin_loc                                     = [90mnull[0m[0m
#     custom_login_page                                    = [90mnull[0m[0m
#     custom_login_page_on                                 = true
#     description                                          = [90mnull[0m[0m
#     form_template                                        = [90mnull[0m[0m
#     grant_types                                          = [
#         "authorization_code",
#         "implicit",
#         "refresh_token",
#     ]
#     id                                                   = "8YECKUMM3I7ejQgsVky40b5LooQB0YFf"
#     initiate_login_uri                                   = [90mnull[0m[0m
#     is_first_party                                       = true
#     is_token_endpoint_ip_header_trusted                  = false
#     logo_uri                                             = [90mnull[0m[0m
#     name                                                 = "mycrocloud"
#     oidc_backchannel_logout_urls                         = []
#     oidc_conformant                                      = true
#     organization_require_behavior                        = "no_prompt"
#     organization_usage                                   = [90mnull[0m[0m
#     require_proof_of_possession                          = false
#     require_pushed_authorization_requests                = false
#     resource_server_identifier                           = [90mnull[0m[0m
#     signing_keys                                         = (sensitive value)
#     skip_non_verifiable_callback_uri_confirmation_prompt = false
#     sso                                                  = false
#     sso_disabled                                         = false
#     web_origins                                          = [
#         "https://mycrocloud.info",
#     ]

#     default_organization {
#         disable         = true
#         flows           = []
#         organization_id = [90mnull[0m[0m
#     }

#     jwt_configuration {
#         alg                 = "RS256"
#         lifetime_in_seconds = 36000
#         scopes              = {}
#         secret_encoded      = false
#     }

#     native_social_login {
#         apple {
#             enabled = false
#         }
#         facebook {
#             enabled = false
#         }
#         google {
#             enabled = false
#         }
#     }

#     refresh_token {
#         expiration_type              = "expiring"
#         idle_token_lifetime          = 1296000
#         infinite_idle_token_lifetime = false
#         infinite_token_lifetime      = false
#         leeway                       = 0
#         rotation_type                = "rotating"
#         token_lifetime               = 2592000
#     }
# }
