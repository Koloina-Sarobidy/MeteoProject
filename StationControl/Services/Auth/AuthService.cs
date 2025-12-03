using System;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;

namespace StationControl.Services.Auth
{
    public class AuthService
    {
        // Cookie
        public static Utilisateur? GetSuperAdminFromCookie(HttpRequest request, MySqlConnection connection)
        {
            if (request.Cookies.TryGetValue("SuperAdminSession", out string userIdStr))
            {
                if (int.TryParse(userIdStr, out int userId))
                {
                    return UtilisateurService.GetUtilisateurById(connection, userId);
                }
            }
            return null;
        }
        public static Utilisateur? GetAdminFromCookie(HttpRequest request, MySqlConnection connection)
        {
            if (request.Cookies.TryGetValue("AdminSession", out string userIdStr))
            {
                if (int.TryParse(userIdStr, out int userId))
                {
                    return UtilisateurService.GetUtilisateurById(connection, userId);
                }
            }

            return null;
        }
        public static Utilisateur? GetObservateurFromCookie(HttpRequest request, MySqlConnection connection)
        {
            if (request.Cookies.TryGetValue("ObservateurSession", out string userIdStr))
            {
                if (int.TryParse(userIdStr, out int userId))
                {
                    return UtilisateurService.GetUtilisateurById(connection, userId);
                }
            }

            return null;
        }


        // Role

        public static Role GetRoleById(MySqlConnection connection, int id)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var query = "SELECT * FROM role WHERE id = @Id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Role
                {
                    Id = reader.GetInt32("id"),
                    Libelle = reader.GetString("libelle"),
                    Description = reader.IsDBNull(reader.GetOrdinal("description"))
                        ? null
                        : reader.GetString("description")
                };
            }
            return null;
        }
        public static Role GetRoleByLibelle(MySqlConnection connection, string libelle)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrEmpty(libelle))
                throw new ArgumentException("Le libellé ne peut pas être vide.", nameof(libelle));

            string query = "SELECT id, libelle, description FROM role WHERE libelle = @Libelle";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Libelle", libelle);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Role
                {
                    Id = reader.GetInt32("id"),
                    Libelle = reader.GetString("libelle"),
                    Description = reader.IsDBNull(reader.GetOrdinal("description"))
                        ? null
                        : reader.GetString("description")
                };
            }

            return null; 
        }
        public static List<Role> GetAllRole(MySqlConnection connection)
        {
            List<Role> roles = new List<Role>();

            string query = @"SELECT id, libelle, description
                             FROM role";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Role role = new Role
                        {
                            Id = reader.GetInt32("id"),
                            Libelle = reader.GetString("libelle"),
                            Description = reader.IsDBNull(reader.GetOrdinal("description"))
                                ? null
                                : reader.GetString("description")
                        };
                        roles.Add(role);
                    }
                }
            }
            return roles;
        }

        // Historique de connexion
        public static void InsertHistoriqueConnexion(MySqlConnection connection, HistoriqueConnexion historique)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (historique == null)
                throw new ArgumentNullException(nameof(historique));
            if (historique.Utilisateur == null)
                throw new ArgumentNullException(nameof(historique.Utilisateur));

            string query = @"
                INSERT INTO historique_connexion (utilisateur_id, date_heure_debut, date_heure_fin)
                VALUES (@UtilisateurId, @DateHeureDebut, @DateHeureFin);
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UtilisateurId", historique.Utilisateur.Id);
            cmd.Parameters.AddWithValue("@DateHeureDebut", historique.DateHeureDebut);
            cmd.Parameters.AddWithValue("@DateHeureFin", historique.DateHeureFin.HasValue 
                ? (object)historique.DateHeureFin.Value 
                : DBNull.Value);

            cmd.ExecuteNonQuery();

            historique.Id = (int)cmd.LastInsertedId;
        }
       public static List<HistoriqueConnexion> GetHistoriqueConnexionByUtilisateur(
            MySqlConnection connection, DateTime? debut, DateTime? fin, int idUtilisateur)
        {
            List<HistoriqueConnexion> historiques = new List<HistoriqueConnexion>();

            if (!debut.HasValue && !fin.HasValue)
            {
                var now = DateTime.Now;
                debut = new DateTime(now.Year, now.Month, 1); 
                fin = debut.Value.AddMonths(1).AddSeconds(-1); 
            }

            string query = @"
                SELECT id, utilisateur_id, date_heure_debut, date_heure_fin
                FROM historique_connexion
                WHERE utilisateur_id = @idUtilisateur
            ";

            if (debut.HasValue)
                query += " AND date_heure_debut >= @debut ";
            if (fin.HasValue)
                query += " AND date_heure_debut <= @fin ";

            query += " ORDER BY date_heure_debut DESC ";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@idUtilisateur", idUtilisateur);

                if (debut.HasValue)
                    cmd.Parameters.AddWithValue("@debut", debut.Value);

                if (fin.HasValue)
                    cmd.Parameters.AddWithValue("@fin", fin.Value);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        HistoriqueConnexion h = new HistoriqueConnexion
                        {
                            Id = reader.GetInt32("id"),
                            DateHeureDebut = reader.GetDateTime("date_heure_debut"),
                            DateHeureFin = reader.IsDBNull(reader.GetOrdinal("date_heure_fin"))
                                ? null
                                : reader.GetDateTime("date_heure_fin"),

                            Utilisateur = new Utilisateur
                            {
                                Id = reader.GetInt32("utilisateur_id")
                            }
                        };

                        historiques.Add(h);
                    }
                }
            }

            foreach (var h in historiques)
            {
                h.Utilisateur = UtilisateurService.GetUtilisateurById(connection, h.Utilisateur.Id);
            }

            return historiques;
        }


        // Login
        private static bool VerifyPassword(string password, string storedHash)
        {
            using var sha256 = SHA256.Create();
            var hashedInput = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return hashedInput == storedHash.Trim();
        }

        public static Utilisateur ToLog(MySqlConnection connection, string email, string motDePasse, Role role)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (role == null) throw new ArgumentNullException(nameof(role));

            Utilisateur utilisateur = null;

            string query = @"
                SELECT id AS utilisateur_id, mot_de_passe 
                FROM utilisateur 
                WHERE email = @Email AND role_id = @RoleId and est_valide = TRUE and statut != 'Affecté' and statut != 'Retraité';
            ";

            int userId = 0;
            string hashedPassword = "";

            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@RoleId", role.Id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        userId = Convert.ToInt32(reader["utilisateur_id"]);
                        hashedPassword = reader["mot_de_passe"].ToString().Trim();
                    }
                }
            }

            if (!string.IsNullOrEmpty(hashedPassword) && VerifyPassword(motDePasse, hashedPassword))
            {
                utilisateur = UtilisateurService.GetUtilisateurById(connection, userId);

                var history = new HistoriqueConnexion
                {
                    Utilisateur = utilisateur,
                    DateHeureDebut = DateTime.Now
                };
                InsertHistoriqueConnexion(connection, history);
            }

            return utilisateur;
        }
        public static HistoriqueConnexion? GetLastActiveByUtilisateur(MySqlConnection connection, int idUtilisateur)
        {
            const string query = @"
                SELECT id, date_heure_debut, date_heure_fin
                FROM historique_connexion
                WHERE utilisateur_id = @IdUtilisateur AND date_heure_fin IS NULL
                ORDER BY date_heure_debut DESC
                LIMIT 1";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@IdUtilisateur", idUtilisateur);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new HistoriqueConnexion
                {
                    Id = reader.GetInt32("id"),
                    DateHeureDebut = reader.GetDateTime("date_heure_debut"),
                    DateHeureFin = reader.IsDBNull(reader.GetOrdinal("date_heure_fin"))
                        ? null
                        : reader.GetDateTime("date_heure_fin"),
                    Utilisateur = new Utilisateur { Id = idUtilisateur }
                };
            }
            return null;
        }
        public static void UpdateDateHeureFin(MySqlConnection connection, HistoriqueConnexion historique, DateTime endDateTime)
        {
            const string query = @"
                UPDATE historique_connexion
                SET date_heure_fin = @EndDateTime
                WHERE id = @Id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@EndDateTime", endDateTime);
            cmd.Parameters.AddWithValue("@Id", historique.Id);

            cmd.ExecuteNonQuery();
            historique.DateHeureFin = endDateTime;
        }
        public static void Logout(HttpResponse response, string cookieName, string connectionString)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            if (string.IsNullOrEmpty(cookieName)) throw new ArgumentNullException(nameof(cookieName));

            if (response.HttpContext.Request.Cookies.TryGetValue(cookieName, out string userIdString) &&
                int.TryParse(userIdString, out int userId))
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                HistoriqueConnexion? lastHistory = GetLastActiveByUtilisateur(connection, userId);

                if (lastHistory != null)
                {
                    UpdateDateHeureFin(connection, lastHistory, DateTime.Now);
                }
            }
            response.Cookies.Delete(cookieName);
        }
    }
}


