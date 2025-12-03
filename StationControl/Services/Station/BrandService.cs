using System;
using MySql.Data.MySqlClient;
using StationControl.Models.Station;

namespace StationControl.Services.Station
{
    public class BrandService
    {
        public static Brand GetBrandById(MySqlConnection connection, int id)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            var query = "SELECT * FROM brand WHERE id = @Id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Brand { Id = reader.GetInt32("id"), Nom = reader.GetString("nom") };
            }
            return null;
        }
        public static void InsertBrand(MySqlConnection connection, Brand brand)
        {
            string query = @"INSERT INTO brand (nom) 
                             VALUES (@libelle);";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@libelle", brand.Nom);
                object result = cmd.ExecuteScalar();
            }
        }
        public static List<Brand> GetAllBrand(MySqlConnection connection)
        {
            List<Brand> brands = new List<Brand>();

            string query = @"SELECT id, nom from brand
                            ORDER BY id;";

            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var brand = new Brand
                        {
                            Id = reader.GetInt32("id"),
                            Nom = reader.GetString("nom"),
                        };

                        brands.Add(brand);
                    }
                }
            }
            return brands;
        }
    }
}
