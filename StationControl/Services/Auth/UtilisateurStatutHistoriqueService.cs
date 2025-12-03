using MySql.Data.MySqlClient;
using StationControl.Models.Auth;
using System;

namespace StationControl.Services.Auth
{
    public static class UtilisateurStatutHistoriqueService
    {
        public static UtilisateurStatutHistorique? GetLastStatutByUtilisateur(MySqlConnection connection, int idUtilisateur)
        {
            string query = @"
                SELECT id, statut, date_debut, date_fin, description, utilisateur_maj_id
                FROM utilisateur_statut_historique
                WHERE utilisateur_id = @idUtilisateur
                ORDER BY date_debut DESC, id DESC
                LIMIT 1;
            ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@idUtilisateur", idUtilisateur);

            UtilisateurStatutHistorique? statutHistorique = null;
            
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    statutHistorique = new UtilisateurStatutHistorique
                    {
                        Id = reader.GetInt32("id"),
                        Statut = reader.GetString("statut"),
                        DateDebut = reader.GetDateTime("date_debut"),
                        DateFin = reader.IsDBNull(reader.GetOrdinal("date_fin")) ? (DateTime?)null : reader.GetDateTime("date_fin"),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                        UtilisateurMaj = reader.IsDBNull(reader.GetOrdinal("utilisateur_maj_id")) ? null : new Utilisateur { Id = reader.GetInt32("utilisateur_maj_id") }
                    };
                }
            }
            
            if (statutHistorique != null)
            {
                statutHistorique.Utilisateur = UtilisateurService.GetUtilisateurById(connection, idUtilisateur);
                
                if (statutHistorique.UtilisateurMaj != null)
                {
                    statutHistorique.UtilisateurMaj = UtilisateurService.GetUtilisateurById(connection, statutHistorique.UtilisateurMaj.Id);
                }
            }
            return statutHistorique;
        }
    }
}
