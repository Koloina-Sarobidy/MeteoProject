
-- Role
INSERT INTO role (libelle, description) VALUES
('Chef SMIT', 'Super Admin'),
('Responsable CRM', 'Admin simple'),
('Observateur', 'Observateur / Responsable station');

-- Congig Statut Station
INSERT INTO config_statut_station (label, min_defauts, max_defauts) VALUES
('Fonctionnelle', 0, 0),                 
('Partiellement Fonctionnelle', 1, 3),                  
('Non Fonctionnelle', 4, NULL);     


-- Region
INSERT INTO region (id, nom) VALUES
(1, 'Analamanga'),
(2, 'Bongolava'),
(3, 'Itasy'),
(4, 'Vakinankaratra'),
(5, 'Alaotra-Mangoro'),
(6, 'Atsinanana'),
(7, 'Analanjirofo'),
(8, 'Amoron\’i Mania'),
(9, 'Haute Matsiatra'),
(10, 'Vatovavy'),
(11, 'Fitovinany'),
(12, 'Atsimo-Atsinanana'),
(13, 'Ihorombe'),
(14, 'Anosy'),
(15, 'Androy'),
(16, 'Atsimo-Andrefana'),
(17, 'Menabe'),
(18, 'Melaky'),
(19, 'Boeny'),
(20, 'Sofia'),
(21, 'Betsiboka'),
(22, 'Diana'),
(23, 'Sava');


-- Type Station
INSERT INTO type_station(libelle) VALUES
('SYNOPTIQUE'),
('AGROMETEOROLOGIQUE');

-- Equipement Besoin
INSERT INTO equipement_besoin (libelle) VALUES
('Remplacement'),
('Calibrage / Etalonnage'),
('Réparation'),
('Autres');

-- Premier Utilisateur (Super Admin)
INSERT INTO utilisateur 
    (nom, prenom, email, mot_de_passe, role_id, genre, date_debut, date_fin, photo_profil, crm_id, station_id, est_valide)
VALUES
    ('RAVONILANTOSOA', 'Koloina Sarobidy', 'koloinasarobidy2742@gmail.com', 'RXkd9UBNKb0/PYOm3+iCGGttmHIwY4DfRu2u0hbdxzw=', 1, 'Féminin', '2025-11-28', NULL, NULL, NULL, NULL, TRUE);


