using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using StationControl.Services.Station;

namespace StationControl.Services.Auth
{
    public static class UtilisateurService
    {
        public static Utilisateur GetUtilisateurById(MySqlConnection connection, int utilisateurId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT id, nom, prenom, email, mot_de_passe, role_id, station_id,
                       crm_id, genre, est_valide, date_debut, date_fin, photo_profil, statut
                FROM utilisateur
                WHERE id = @Id
            ";

            int? roleId;
            int? stationId;
            int? crmId;
            string nom;
            string prenom;
            string email;
            string motDePasse;
            string genre;
            bool estValide;
            DateTime dateDebut;
            DateTime? dateFin;
            string photoProfil;
            string statut;

            // Lecture des données dans des variables temporaires
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", utilisateurId);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return null;

                roleId = reader.IsDBNull("role_id") ? null : reader.GetInt32("role_id");
                stationId = reader.IsDBNull("station_id") ? null : reader.GetInt32("station_id");
                crmId = reader.IsDBNull("crm_id") ? null : reader.GetInt32("crm_id");
                nom = reader.GetString("nom");
                prenom = reader.GetString("prenom");
                email = reader.GetString("email");
                motDePasse = reader.GetString("mot_de_passe");
                genre = reader.IsDBNull("genre") ? null : reader.GetString("genre");
                estValide = reader.GetBoolean("est_valide");
                dateDebut = reader.GetDateTime("date_debut");
                dateFin = reader.IsDBNull("date_fin") ? null : reader.GetDateTime("date_fin");
                photoProfil = reader.IsDBNull("photo_profil") ? null : reader.GetString("photo_profil");
                statut = reader.IsDBNull("statut") ? "Actif" : reader.GetString("statut");
            }

            // Lecture du rôle en dehors du reader
            Role role = roleId.HasValue ? AuthService.GetRoleById(connection, roleId.Value) : null;

            return new Utilisateur
            {
                Id = utilisateurId,
                Nom = nom,
                Prenom = prenom,
                Email = email,
                MotDePasse = motDePasse,
                Role = role,
                StationId = stationId,
                CrmId = crmId,
                Genre = genre,
                EstValide = estValide,
                DateDebut = dateDebut,
                DateFin = dateFin,
                PhotoProfil = photoProfil,
                Statut = statut
            };
        }

        public static void InsertUtilisateur(MySqlConnection connection, Utilisateur utilisateur)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (utilisateur == null) throw new ArgumentNullException(nameof(utilisateur));
            if (utilisateur.Role == null) throw new ArgumentNullException(nameof(utilisateur.Role));

            string hashedPassword;
            using (var sha256 = SHA256.Create())
                hashedPassword = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(utilisateur.MotDePasse)));

            string query = @"
                INSERT INTO utilisateur
                (nom, prenom, email, mot_de_passe, role_id, genre, date_debut, date_fin, photo_profil, crm_id, station_id, est_valide, statut)
                VALUES
                (@Nom, @Prenom, @Email, @MotDePasse, @RoleId, @Genre, @DateDebut, @DateFin, @PhotoProfil, @CrmId, @StationId, @EstValide, @Statut);
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Nom", utilisateur.Nom);
            cmd.Parameters.AddWithValue("@Prenom", utilisateur.Prenom);
            cmd.Parameters.AddWithValue("@Email", utilisateur.Email);
            cmd.Parameters.AddWithValue("@MotDePasse", hashedPassword);
            cmd.Parameters.AddWithValue("@RoleId", utilisateur.Role.Id);
            cmd.Parameters.AddWithValue("@Genre", utilisateur.Genre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DateDebut", utilisateur.DateDebut);
            cmd.Parameters.AddWithValue("@DateFin", utilisateur.DateFin.HasValue ? (object)utilisateur.DateFin.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@PhotoProfil", string.IsNullOrEmpty(utilisateur.PhotoProfil) ? DBNull.Value : utilisateur.PhotoProfil);
            cmd.Parameters.AddWithValue("@CrmId", utilisateur.CrmId.HasValue ? (object)utilisateur.CrmId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@StationId", utilisateur.StationId.HasValue ? (object)utilisateur.StationId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@EstValide", utilisateur.EstValide);
            cmd.Parameters.AddWithValue("@Statut", string.IsNullOrWhiteSpace(utilisateur.Statut) ? "Actif" : utilisateur.Statut);

            cmd.ExecuteNonQuery();
            utilisateur.Id = (int)cmd.LastInsertedId;
        }

        public static void UpdateUtilisateur(MySqlConnection connection, Utilisateur utilisateur)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (utilisateur == null) throw new ArgumentNullException(nameof(utilisateur));
            if (utilisateur.Role == null) throw new ArgumentNullException(nameof(utilisateur.Role));

            string query = @"
                UPDATE utilisateur
                SET
                    nom = @Nom,
                    prenom = @Prenom,
                    email = @Email,
                    role_id = @RoleId,
                    genre = @Genre,
                    date_debut = @DateDebut,
                    date_fin = @DateFin,
                    photo_profil = @PhotoProfil,
                    crm_id = @CrmId,
                    station_id = @StationId,
                    est_valide = @EstValide,
                    statut = @Statut,
                    date_maj = CURRENT_TIMESTAMP
                WHERE id = @Id
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Nom", utilisateur.Nom);
            cmd.Parameters.AddWithValue("@Prenom", utilisateur.Prenom);
            cmd.Parameters.AddWithValue("@Email", utilisateur.Email);
            cmd.Parameters.AddWithValue("@RoleId", utilisateur.Role.Id);
            cmd.Parameters.AddWithValue("@Genre", utilisateur.Genre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DateDebut", utilisateur.DateDebut);
            cmd.Parameters.AddWithValue("@DateFin", utilisateur.DateFin.HasValue ? (object)utilisateur.DateFin.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@PhotoProfil", string.IsNullOrEmpty(utilisateur.PhotoProfil) ? DBNull.Value : utilisateur.PhotoProfil);
            cmd.Parameters.AddWithValue("@CrmId", utilisateur.CrmId.HasValue ? (object)utilisateur.CrmId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@StationId", utilisateur.StationId.HasValue ? (object)utilisateur.StationId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@EstValide", utilisateur.EstValide);
            cmd.Parameters.AddWithValue("@Statut", string.IsNullOrWhiteSpace(utilisateur.Statut) ? "Actif" : utilisateur.Statut);
            cmd.Parameters.AddWithValue("@Id", utilisateur.Id);

            cmd.ExecuteNonQuery();
        }

        public static void UpdateMotDePasse(MySqlConnection connection, int utilisateurId, string nouveauMotDePasse)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(nouveauMotDePasse))
                throw new ArgumentException("Le mot de passe ne peut pas être vide.", nameof(nouveauMotDePasse));

            string hashedPassword;
            using (var sha256 = SHA256.Create())
                hashedPassword = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(nouveauMotDePasse)));

            string query = @"
                UPDATE utilisateur
                SET mot_de_passe = @MotDePasse, date_maj = CURRENT_TIMESTAMP
                WHERE id = @Id
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@MotDePasse", hashedPassword);
            cmd.Parameters.AddWithValue("@Id", utilisateurId);

            cmd.ExecuteNonQuery();
        }

        public static void ValiderUtilisateur(MySqlConnection connection, int utilisateurId)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            string query = @"UPDATE utilisateur SET est_valide = 1 WHERE id = @Id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", utilisateurId);
            cmd.ExecuteNonQuery();
        }

        public static bool DeleteUtilisateur(MySqlConnection connection, int utilisateurId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"DELETE FROM utilisateur WHERE id = @Id";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", utilisateurId);

            int rows = cmd.ExecuteNonQuery();
            return rows > 0; 
        }

        private static List<Utilisateur> ReadUtilisateurs(MySqlConnection connection, MySqlCommand cmd)
        {
            var tempList = new List<(int id, int? roleId, string nom, string prenom, string email, string motDePasse,
                                     int? stationId, int? crmId, string genre, bool estValide, DateTime dateDebut,
                                     DateTime? dateFin, string photoProfil, string statut)>();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tempList.Add((
                        id: reader.GetInt32("id"),
                        roleId: reader.IsDBNull("role_id") ? null : reader.GetInt32("role_id"),
                        nom: reader.GetString("nom"),
                        prenom: reader.GetString("prenom"),
                        email: reader.GetString("email"),
                        motDePasse: reader.GetString("mot_de_passe"),
                        stationId: reader.IsDBNull("station_id") ? null : reader.GetInt32("station_id"),
                        crmId: reader.IsDBNull("crm_id") ? null : reader.GetInt32("crm_id"),
                        genre: reader.IsDBNull("genre") ? null : reader.GetString("genre"),
                        estValide: reader.GetBoolean("est_valide"),
                        dateDebut: reader.GetDateTime("date_debut"),
                        dateFin: reader.IsDBNull("date_fin") ? null : reader.GetDateTime("date_fin"),
                        photoProfil: reader.IsDBNull("photo_profil") ? null : reader.GetString("photo_profil"),
                        statut: reader.IsDBNull("statut") ? "Actif" : reader.GetString("statut")
                    ));
                }
            }

            var utilisateurs = new List<Utilisateur>();
            foreach (var u in tempList)
            {
                Role role = u.roleId.HasValue ? AuthService.GetRoleById(connection, u.roleId.Value) : null;
                utilisateurs.Add(new Utilisateur
                {
                    Id = u.id,
                    Nom = u.nom,
                    Prenom = u.prenom,
                    Email = u.email,
                    MotDePasse = u.motDePasse,
                    Role = role,
                    StationId = u.stationId,
                    CrmId = u.crmId,
                    Genre = u.genre,
                    EstValide = u.estValide,
                    DateDebut = u.dateDebut,
                    DateFin = u.dateFin,
                    PhotoProfil = u.photoProfil,
                    Statut = u.statut
                });
            }

            return utilisateurs;
        }

        public static List<Utilisateur> GetObservateursNonValides(MySqlConnection connection)
        {
            string query = @"
                SELECT id, nom, prenom, email, mot_de_passe, role_id, station_id,
                       crm_id, genre, est_valide, date_debut, date_fin, photo_profil, statut
                FROM utilisateur
                WHERE est_valide = 0 AND role_id = (SELECT id FROM role WHERE libelle = 'Observateur' LIMIT 1)
            ";

            using var cmd = new MySqlCommand(query, connection);
            return ReadUtilisateurs(connection, cmd);
        }
        public static List<Utilisateur> GetSuperAdminNonValides(MySqlConnection connection)
        {
            string query = @"
                SELECT id, nom, prenom, email, mot_de_passe, role_id, station_id,
                       crm_id, genre, est_valide, date_debut, date_fin, photo_profil, statut
                FROM utilisateur
                WHERE est_valide = 0 AND role_id = (SELECT id FROM role WHERE libelle = 'Chef SMIT' LIMIT 1)
            ";

            using var cmd = new MySqlCommand(query, connection);
            return ReadUtilisateurs(connection, cmd);
        }


        public static List<Utilisateur> GetResponsablesCRMNonValides(MySqlConnection connection)
        {
            string query = @"
                SELECT id, nom, prenom, email, mot_de_passe, role_id, station_id,
                       crm_id, genre, est_valide, date_debut, date_fin, photo_profil, statut
                FROM utilisateur
                WHERE est_valide = 0 AND role_id = (SELECT id FROM role WHERE libelle = 'Responsable CRM' LIMIT 1)
            ";

            using var cmd = new MySqlCommand(query, connection);
            return ReadUtilisateurs(connection, cmd);
        }

        public static List<Utilisateur> GetUtilisateurByRole(MySqlConnection connection, string roleLibelle)
        {
            string query = @"
                SELECT u.id, u.nom, u.prenom, u.email, u.mot_de_passe, u.role_id, u.station_id,
                       u.crm_id, u.genre, u.est_valide, u.date_debut, u.date_fin, u.photo_profil, u.statut
                FROM utilisateur u
                INNER JOIN role r ON u.role_id = r.id
                WHERE r.libelle = @RoleLibelle
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@RoleLibelle", roleLibelle);
            return ReadUtilisateurs(connection, cmd);
        }

        public static List<Utilisateur> GetResponsablesCrmFiltres(MySqlConnection connection, int? crmId, DateTime? dateDebut, string statut)
        {
            var conditions = new List<string>();
            string baseQuery = @"
                SELECT id, nom, prenom, email, mot_de_passe, role_id, station_id,
                       crm_id, genre, est_valide, date_debut, date_fin, photo_profil, statut
                FROM utilisateur
                WHERE role_id = (SELECT id FROM role WHERE libelle = 'Responsable CRM' LIMIT 1)
            ";

            if (crmId.HasValue) conditions.Add("crm_id = @CrmId");
            if (dateDebut.HasValue) conditions.Add("DATE(date_debut) >= DATE(@DateDebut)");
            if (!string.IsNullOrWhiteSpace(statut)) conditions.Add("statut = @Statut");

            if (conditions.Count > 0)
                baseQuery += " AND " + string.Join(" AND ", conditions);

            baseQuery += " ORDER BY date_debut DESC";

            using var cmd = new MySqlCommand(baseQuery, connection);
            if (crmId.HasValue) cmd.Parameters.AddWithValue("@CrmId", crmId.Value);
            if (dateDebut.HasValue) cmd.Parameters.AddWithValue("@DateDebut", dateDebut.Value);
            if (!string.IsNullOrWhiteSpace(statut)) cmd.Parameters.AddWithValue("@Statut", statut);

            return ReadUtilisateurs(connection, cmd);
        }
        
        public static List<Utilisateur> GetObservateursFiltres(MySqlConnection connection, int? stationId, DateTime? dateDebut, string statut)
        {
            var conditions = new List<string>();
            string baseQuery = @"
                SELECT id, nom, prenom, email, mot_de_passe, role_id, station_id,
                    crm_id, genre, est_valide, date_debut, date_fin, photo_profil, statut
                FROM utilisateur
                WHERE role_id = (SELECT id FROM role WHERE libelle = 'Observateur' LIMIT 1)
            ";

            if (stationId.HasValue) conditions.Add("station_id = @StationId");
            if (dateDebut.HasValue) conditions.Add("DATE(date_debut) >= DATE(@DateDebut)");
            if (!string.IsNullOrWhiteSpace(statut)) conditions.Add("statut = @Statut");

            if (conditions.Count > 0)
                baseQuery += " AND " + string.Join(" AND ", conditions);

            baseQuery += " ORDER BY date_debut DESC";

            using var cmd = new MySqlCommand(baseQuery, connection);
            if (stationId.HasValue) cmd.Parameters.AddWithValue("@StationId", stationId.Value);
            if (dateDebut.HasValue) cmd.Parameters.AddWithValue("@DateDebut", dateDebut.Value);
            if (!string.IsNullOrWhiteSpace(statut)) cmd.Parameters.AddWithValue("@Statut", statut);

            return ReadUtilisateurs(connection, cmd);
        }

        public static void ModifierStatut(
            MySqlConnection connection, 
            int utilisateurId, 
            string nouveauStatut, 
            int utilisateurMajId,
            string? description = null,
            DateTime? dateDebut = null,
            DateTime? dateFin = null)
        {
            DateTime debut = dateDebut ?? DateTime.Today;

            string closeQuery = @"
                UPDATE utilisateur_statut_historique
                SET date_fin = @Today
                WHERE utilisateur_id = @Id AND date_fin IS NULL";

            using (var cmdClose = new MySqlCommand(closeQuery, connection))
            {
                cmdClose.Parameters.AddWithValue("@Id", utilisateurId);
                cmdClose.Parameters.AddWithValue("@Today", DateTime.Today);
                cmdClose.ExecuteNonQuery();
            }

            string insertQuery = @"
                INSERT INTO utilisateur_statut_historique 
                (utilisateur_id, statut, date_debut, date_fin, utilisateur_maj_id, description)
                VALUES (@Id, @Statut, @Debut, @Fin, @UserMaj, @Desc)";

            using (var cmdInsert = new MySqlCommand(insertQuery, connection))
            {
                cmdInsert.Parameters.AddWithValue("@Id", utilisateurId);
                cmdInsert.Parameters.AddWithValue("@Statut", nouveauStatut);
                cmdInsert.Parameters.AddWithValue("@Debut", debut);
                cmdInsert.Parameters.AddWithValue("@Fin", dateFin.HasValue ? (object)dateFin.Value : DBNull.Value);
                cmdInsert.Parameters.AddWithValue("@UserMaj", utilisateurMajId);
                cmdInsert.Parameters.AddWithValue("@Desc", description ?? "");
                cmdInsert.ExecuteNonQuery();
            }

            string updateUserQuery = @"
                UPDATE utilisateur
                SET statut = @Statut
                WHERE id = @Id";

            using (var cmdUpdate = new MySqlCommand(updateUserQuery, connection))
            {
                cmdUpdate.Parameters.AddWithValue("@Id", utilisateurId);
                cmdUpdate.Parameters.AddWithValue("@Statut", nouveauStatut);
                cmdUpdate.ExecuteNonQuery();
            }
        }

        public static Utilisateur? GetUserByEmail(string emailToVerify, Role roleUser, MySqlConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT id, nom, prenom, email, mot_de_passe, role_id, station_id,
                       crm_id, genre, est_valide, date_debut, date_fin, photo_profil, statut
                FROM utilisateur
                WHERE email = @Email and role_id = @Role;
            ";

            int? roleId;
            int? stationId;
            int? crmId;
            string nom;
            string prenom;
            string email;
            string motDePasse;
            string genre;
            bool estValide;
            DateTime dateDebut;
            DateTime? dateFin;
            string photoProfil;
            string statut;
            int utilisateurId;

            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Email", emailToVerify);
                cmd.Parameters.AddWithValue("@Role", roleUser.Id);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return null;

                utilisateurId = reader.GetInt32("id");
                roleId = reader.IsDBNull("role_id") ? null : reader.GetInt32("role_id");
                stationId = reader.IsDBNull("station_id") ? null : reader.GetInt32("station_id");
                crmId = reader.IsDBNull("crm_id") ? null : reader.GetInt32("crm_id");
                nom = reader.GetString("nom");
                prenom = reader.GetString("prenom");
                email = reader.GetString("email");
                motDePasse = reader.GetString("mot_de_passe");
                genre = reader.IsDBNull("genre") ? null : reader.GetString("genre");
                estValide = reader.GetBoolean("est_valide");
                dateDebut = reader.GetDateTime("date_debut");
                dateFin = reader.IsDBNull("date_fin") ? null : reader.GetDateTime("date_fin");
                photoProfil = reader.IsDBNull("photo_profil") ? null : reader.GetString("photo_profil");
                statut = reader.IsDBNull("statut") ? "Actif" : reader.GetString("statut");
            }

            Role role = roleId.HasValue ? AuthService.GetRoleById(connection, roleId.Value) : null;

            return new Utilisateur
            {
                Id = utilisateurId,
                Nom = nom,
                Prenom = prenom,
                Email = email,
                MotDePasse = motDePasse,
                Role = role,
                StationId = stationId,
                CrmId = crmId,
                Genre = genre,
                EstValide = estValide,
                DateDebut = dateDebut,
                DateFin = dateFin,
                PhotoProfil = photoProfil,
                Statut = statut
            };
        }

        public static List<Utilisateur> GetUtilisateurByStation(MySqlConnection connection, int stationId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT id, nom, prenom, email, mot_de_passe, role_id, station_id,
                    crm_id, genre, est_valide, date_debut, date_fin, photo_profil, statut
                FROM utilisateur
                WHERE station_id = @StationId
                ORDER BY nom, prenom
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@StationId", stationId);

            return ReadUtilisateurs(connection, cmd);
        }

        public static List<Utilisateur> GetObservateursNonValidesByStation(MySqlConnection connection, int stationId)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            string query = @"
                SELECT id, nom, prenom, email, mot_de_passe, role_id, station_id,
                    crm_id, genre, est_valide, date_debut, date_fin, photo_profil, statut
                FROM utilisateur
                WHERE est_valide = 0
                AND station_id = @StationId
                AND role_id = (SELECT id FROM role WHERE libelle = 'Observateur' LIMIT 1)
                ORDER BY nom, prenom
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@StationId", stationId);

            return ReadUtilisateurs(connection, cmd);
        }

        
    }
}
