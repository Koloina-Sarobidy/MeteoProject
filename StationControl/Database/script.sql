/******************************************************************************************
    0 — RESET COMPLET DE LA BASE (OPTIONNEL)
******************************************************************************************/
--  Supprimer toutes les procédures
SELECT GROUP_CONCAT(CONCAT('DROP PROCEDURE IF EXISTS `', routine_name, '`;') SEPARATOR ' ')
INTO @drop_procs
FROM information_schema.routines
WHERE routine_schema = 'mydb'
  AND routine_type = 'PROCEDURE';

SET @drop_procs = IFNULL(@drop_procs, 'SELECT "Aucune procédure à supprimer";');

PREPARE stmt FROM @drop_procs;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

--  Supprimer toutes les VIEWS
SET @views = NULL;
SELECT GROUP_CONCAT('`', table_name, '`') INTO @views
FROM information_schema.views
WHERE table_schema = 'mydb';

SET @sql = IFNULL(CONCAT('DROP VIEW IF EXISTS ', @views), 'SELECT "no views found"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

--  Supprimer toutes les TABLES
SET FOREIGN_KEY_CHECKS = 0;

SET @tables = NULL;
SELECT GROUP_CONCAT('`', table_name, '`') INTO @tables
FROM information_schema.tables
WHERE table_schema = 'mydb'
AND table_type = 'BASE TABLE';

SET @sql = IFNULL(CONCAT('DROP TABLE IF EXISTS ', @tables), 'SELECT "no tables found"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET FOREIGN_KEY_CHECKS = 1;


/******************************************************************************************
    1 — TABLES DE BASE (AUCUNE DÉPENDANCE)
******************************************************************************************/
-- Rôle utilisateur
CREATE TABLE role (
    id INT AUTO_INCREMENT PRIMARY KEY,
    libelle VARCHAR(100) NOT NULL,
    description TEXT
);

-- Région géographique
CREATE TABLE region (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nom VARCHAR(100) NOT NULL
);

-- CRM
CREATE TABLE crm (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nom VARCHAR(100) NOT NULL,
    date_debut DATE NOT NULL,
    date_fin DATE,
    est_arrete BOOLEAN DEFAULT FALSE,
    date_maj TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Capteur
CREATE TABLE capteur (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nom VARCHAR(100) NOT NULL,
    parametre VARCHAR(100)
);

-- Alimentation
CREATE TABLE alimentation (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nom VARCHAR(100) NOT NULL,
    description TEXT
);

-- Marque (brand)
CREATE TABLE brand (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nom VARCHAR(100) NOT NULL
);

-- Type Station
CREATE TABLE type_station (
    id INT AUTO_INCREMENT PRIMARY KEY,
    libelle VARCHAR(100) NOT NULL
);

-- Config Statut Station
CREATE TABLE config_statut_station (
    id INT AUTO_INCREMENT PRIMARY KEY,
    label VARCHAR(50) NOT NULL,        
    min_defauts INT NOT NULL,          
    max_defauts INT NULL                
);

            


/******************************************************************************************
    2 — UTILISATEURS (DÉPEND DE role ET crm)
******************************************************************************************/
CREATE TABLE utilisateur (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nom VARCHAR(100) NOT NULL,
    prenom VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL,
    mot_de_passe VARCHAR(255) NOT NULL,
    role_id INT NOT NULL,
    genre ENUM('Masculin', 'Féminin', 'Autres'),
    date_debut DATE NOT NULL,
    date_fin DATE NULL,
    photo_profil VARCHAR(255) NULL,
    crm_id INT NULL,
    station_id INT NULL,
    est_valide BOOLEAN DEFAULT FALSE,
    statut ENUM('Actif','Congé','Retraité','Affecté') NOT NULL DEFAULT 'Actif',
    date_maj DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (role_id) REFERENCES role(id),
    FOREIGN KEY (crm_id) REFERENCES crm(id)
);

CREATE TABLE historique_connexion (
    id INT AUTO_INCREMENT PRIMARY KEY,
    utilisateur_id INT NOT NULL,
    date_heure_debut DATETIME NOT NULL,
    date_heure_fin DATETIME,
    FOREIGN KEY (utilisateur_id) REFERENCES utilisateur(id)
);


/******************************************************************************************
    3 — STATION (DÉPEND region, type_station **ET importance SUPPRIMÉE**)
******************************************************************************************/
CREATE TABLE station (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nom VARCHAR(100) NOT NULL,
    latitude DECIMAL(10, 6),
    longitude DECIMAL(10, 6),
    type_station_id INT,
    region_id INT,
    brand_id INT NULL,
    statut ENUM('Non Fonctionnelle', 'Fonctionnelle', 'Partiellement Fonctionnelle'),
    date_debut DATE NOT NULL,
    date_fin DATE,
    est_arrete BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (type_station_id) REFERENCES type_station(id),
    FOREIGN KEY (region_id) REFERENCES region(id),
    FOREIGN KEY (brand_id) REFERENCES brand(id)
);

-- Mise à jour automatique statut station
DELIMITER $$

CREATE PROCEDURE update_station_statut_proc(IN p_station_id INT)
BEGIN
    DECLARE total_defauts INT DEFAULT 0;
    DECLARE new_statut VARCHAR(50);

    SELECT COUNT(*) INTO total_defauts
    FROM equipement_station
    WHERE station_id = p_station_id
      AND est_remplace = FALSE
      AND statut <> 'Fonctionnel';

    SELECT label INTO new_statut
    FROM config_statut_station
    WHERE min_defauts <= total_defauts
      AND (max_defauts IS NULL OR total_defauts <= max_defauts)
    LIMIT 1;

    UPDATE station
    SET statut = new_statut
    WHERE id = p_station_id;
END$$

DELIMITER ;


-- Trigger pour déclencher la mis à jour automatique du statut de la station 
DELIMITER $$

CREATE TRIGGER tr_update_station_statut
AFTER UPDATE ON equipement_station
FOR EACH ROW
BEGIN
    IF NEW.statut <> OLD.statut OR NEW.est_remplace <> OLD.est_remplace THEN
        CALL update_station_statut_proc(NEW.station_id);
    END IF;
END$$

DELIMITER ;






/******************************************************************************************
    4 — TABLES LIEES AUX CRM
******************************************************************************************/
CREATE TABLE crm_station (
    id INT AUTO_INCREMENT PRIMARY KEY,
    crm_id INT NOT NULL,
    station_id INT NOT NULL,
    date_maj TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (crm_id) REFERENCES crm(id),
    FOREIGN KEY (station_id) REFERENCES station(id)
);


/******************************************************************************************
    5 — INTERVENTION ET PREVENTIVE
******************************************************************************************/
CREATE TABLE preventive (
    id INT AUTO_INCREMENT PRIMARY KEY,
    station_id INT NOT NULL,
    utilisateur_planificateur_id INT NOT NULL,
    utilisateur_effectif_id INT,
    date_prevue_debut DATE NOT NULL,
    date_prevue_fin DATE,
    date_effective_debut DATE,
    date_effective_fin DATE,
    est_complete BOOLEAN DEFAULT FALSE,
    est_annule BOOLEAN DEFAULT FALSE,
    description TEXT,
    FOREIGN KEY (station_id) REFERENCES station(id),
    FOREIGN KEY (utilisateur_planificateur_id) REFERENCES utilisateur(id),
    FOREIGN KEY (utilisateur_effectif_id) REFERENCES utilisateur(id)
);

CREATE TABLE utilisateur_statut_historique (
    id INT AUTO_INCREMENT PRIMARY KEY,
    utilisateur_id INT NOT NULL,
    statut ENUM('Actif', 'Congé', 'Retraité', 'Affecté') NOT NULL,
    date_debut DATE NOT NULL,
    date_fin DATE NULL,
    utilisateur_maj_id INT,
    description TEXT,
    FOREIGN KEY (utilisateur_id) REFERENCES utilisateur(id),
    FOREIGN KEY (utilisateur_maj_id) REFERENCES utilisateur(id)
);


/******************************************************************************************
    6 — TICKETS
******************************************************************************************/
CREATE TABLE ticket (
    id INT AUTO_INCREMENT PRIMARY KEY,
    objet VARCHAR(255) NOT NULL,
    description TEXT,
    date_creation DATETIME NOT NULL,
    utilisateur_id INT NOT NULL,
    crm_id INT NULL,
    station_id INT NULL,
    super_admin BOOLEAN NOT NULL DEFAULT false,
    FOREIGN KEY (utilisateur_id) REFERENCES utilisateur(id),
    FOREIGN KEY (crm_id) REFERENCES crm(id) ON DELETE SET NULL,
    FOREIGN KEY (station_id) REFERENCES station(id) ON DELETE SET NULL
);

CREATE TABLE ticket_piece_jointe (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ticket_id INT NOT NULL,
    url TEXT,
    FOREIGN KEY (ticket_id) REFERENCES ticket(id)
);

CREATE TABLE ticket_visibilite (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ticket_id INT NOT NULL,
    super_admin BOOLEAN,
    crm_id INT NULL,
    station_id INT NULL,
    FOREIGN KEY (ticket_id) REFERENCES ticket(id) ON DELETE CASCADE,
    FOREIGN KEY (crm_id) REFERENCES crm(id) ON DELETE SET NULL,
    FOREIGN KEY (station_id) REFERENCES station(id) ON DELETE SET NULL
);

CREATE TABLE ticket_vue (
    id INT AUTO_INCREMENT PRIMARY KEY,
    ticket_id INT NOT NULL,
    super_admin BOOLEAN,
    crm_id INT NULL,
    station_id INT NULL,
    date_vue DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ticket_id) REFERENCES ticket(id) ON DELETE CASCADE,
    FOREIGN KEY (crm_id) REFERENCES crm(id) ON DELETE SET NULL,
    FOREIGN KEY (station_id) REFERENCES station(id) ON DELETE SET NULL
);


/******************************************************************************************
    7 — CODE MOT DE PASSE
******************************************************************************************/
CREATE TABLE code_mot_de_passe (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    utilisateur_id INT NOT NULL,
    code VARCHAR(10) NOT NULL,
    date_expiration DATETIME NOT NULL,
    est_utilise BOOLEAN DEFAULT FALSE
);


/******************************************************************************************
    8 — INVENTAIRE ET FREQUENCES
******************************************************************************************/
CREATE TABLE frequence_inventaire (
    id INT AUTO_INCREMENT PRIMARY KEY,
    station_id INT,
    frequence_jour INT DEFAULT 7,
    date_dernier_inventaire DATE,
    FOREIGN KEY (station_id) REFERENCES station(id)
);

CREATE TABLE inventaire (
    id INT AUTO_INCREMENT PRIMARY KEY,
    station_id INT NOT NULL,
    date_inventaire DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    utilisateur_id INT NOT NULL,
    commentaire TEXT,
    FOREIGN KEY (station_id) REFERENCES station(id),
    FOREIGN KEY (utilisateur_id) REFERENCES utilisateur(id)
);

DELIMITER $$
CREATE TRIGGER tr_update_frequence_inventaire
AFTER INSERT ON inventaire
FOR EACH ROW
BEGIN
    UPDATE frequence_inventaire
    SET date_dernier_inventaire = NEW.date_inventaire
    WHERE station_id = NEW.station_id;
END$$
DELIMITER ;


/******************************************************************************************
    9 — EQUIPEMENTS + INVENTAIRE DETAILS + BESOINS STATION
******************************************************************************************/

CREATE TABLE equipement_besoin (
    id INT AUTO_INCREMENT PRIMARY KEY,
    libelle VARCHAR(200)
);

CREATE TABLE equipement_station (
    id INT AUTO_INCREMENT PRIMARY KEY,
    station_id INT NOT NULL,
    capteur_id INT NULL,
    alimentation_id INT NULL,
    num_serie VARCHAR(100) UNIQUE NOT NULL,
    date_debut DATE NOT NULL,
    date_fin DATE NULL,
    estimation_vie_annee DECIMAL(5,2) DEFAULT NULL,
    est_alimentation BOOLEAN DEFAULT FALSE,
    statut VARCHAR(200) DEFAULT 'Fonctionnel',
    est_remplace BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (station_id) REFERENCES station(id),
    FOREIGN KEY (capteur_id) REFERENCES capteur(id),
    FOREIGN KEY (alimentation_id) REFERENCES alimentation(id)
);

CREATE TABLE inventaire_details (
    id INT AUTO_INCREMENT PRIMARY KEY,
    inventaire_id INT NULL,
    equipement_station_id INT NOT NULL,
    est_fonctionnel BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (equipement_station_id) REFERENCES equipement_station(id),
    FOREIGN KEY (inventaire_id) REFERENCES inventaire(id)
);

CREATE TABLE besoin_station (
    id INT AUTO_INCREMENT PRIMARY KEY,
    station_id INT,
    date DATETIME NOT NULL,
    equipement_station_id INT NOT NULL,
    equipement_besoin_id INT NOT NULL,
    description_probleme TEXT,
    est_renvoye BOOLEAN DEFAULT FALSE,
    est_traite BOOLEAN DEFAULT FALSE,
    est_complete BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (station_id) REFERENCES station(id),
    FOREIGN KEY (equipement_station_id) REFERENCES equipement_station(id),
    FOREIGN KEY (equipement_besoin_id) REFERENCES equipement_besoin(id)
);


/******************************************************************************************
    10 — INTERVENTION
******************************************************************************************/
CREATE TABLE intervention (
    id INT AUTO_INCREMENT PRIMARY KEY,
    station_id INT NOT NULL,
    date DATE,
    besoin_station_id INT,
    utilisateur_planification_id INT NULL,
    utilisateur_effectif_id INT NULL,
    date_planifiee_debut DATETIME NOT NULL,
    date_planifiee_fin DATETIME,
    date_effective_debut DATETIME NULL,
    date_effective_fin DATETIME NULL,
    technicien_planifie VARCHAR(200),
    technicien_effectif VARCHAR(200),
    statut ENUM('Planifiée', 'En Cours', 'Terminée', 'Annulée') DEFAULT 'Planifiée',
    FOREIGN KEY (station_id) REFERENCES station(id),
    FOREIGN KEY (besoin_station_id) REFERENCES besoin_station(id),
    FOREIGN KEY (utilisateur_planification_id) REFERENCES utilisateur(id),
    FOREIGN KEY (utilisateur_effectif_id) REFERENCES utilisateur(id)
);


/******************************************************************************************
    11 — VUE COMPLET STATION
******************************************************************************************/
CREATE OR REPLACE VIEW vue_stations_complet AS
SELECT
    crm.nom AS crm,
    r.nom AS region,
    s.nom AS city_name,
    ts.libelle AS type,
    s.longitude,
    s.latitude,
    CASE
        WHEN s.statut = 'Fonctionnelle' THEN 'F'
        WHEN s.statut = 'Non Fonctionnelle' THEN 'NF'
        ELSE 'PF'
    END AS etat,
    b.nom AS manufacturer_brand,
    CONCAT(
        IF(c.id IS NOT NULL, 'Capteurs ', ''),
        IF(a.id IS NOT NULL, 'Alimentation ', ''),
        IF(c.id IS NULL AND a.id IS NULL, 'À vérifier', '')
    ) AS action_a_entreprendre,
    es.id AS equipement_station_id,
    COALESCE(c.nom, a.nom) AS equipement_libelle,
    es.statut AS equipement_statut,
    es.est_alimentation
FROM station s
LEFT JOIN crm_station cs ON cs.station_id = s.id
LEFT JOIN crm ON crm.id = cs.crm_id
LEFT JOIN region r ON r.id = s.region_id
LEFT JOIN type_station ts ON ts.id = s.type_station_id
LEFT JOIN brand b ON b.id = s.brand_id
LEFT JOIN equipement_station es ON es.station_id = s.id
LEFT JOIN capteur c ON c.id = es.capteur_id
LEFT JOIN alimentation a ON a.id = es.alimentation_id
WHERE s.est_arrete != TRUE;



-- Rapport mensuel
CREATE OR REPLACE VIEW rapport_mensuel_station AS
SELECT
    s.nom AS station_nom,
    s.latitude,
    s.longitude,
    ts.libelle AS type_station,
    r.nom AS region,
    i.date_planifiee_debut,
    i.date_planifiee_fin,
    i.date_effective_debut,
    i.date_effective_fin,
    i.statut AS statut_intervention,
    i.technicien_planifie,
    i.technicien_effectif,
    bs.description_probleme,
    eb.libelle AS equipement_besoin_libelle,
    es.num_serie AS equipement_num_serie,
    COALESCE(c.nom, a.nom) AS equipement_libelle,
    es.statut AS equipement_statut,
    es.est_alimentation
FROM station s
LEFT JOIN type_station ts ON ts.id = s.type_station_id
LEFT JOIN region r ON r.id = s.region_id
LEFT JOIN intervention i ON i.station_id = s.id
LEFT JOIN besoin_station bs ON bs.id = i.besoin_station_id
LEFT JOIN equipement_besoin eb ON eb.id = bs.equipement_besoin_id
LEFT JOIN equipement_station es ON es.id = bs.equipement_station_id
LEFT JOIN capteur c ON c.id = es.capteur_id
LEFT JOIN alimentation a ON a.id = es.alimentation_id;



/******************************************************************************************
    TABLE AUDIT LOG
******************************************************************************************/
CREATE TABLE audit_log (
    id INT AUTO_INCREMENT PRIMARY KEY,
    table_name VARCHAR(100) NOT NULL,
    record_id INT NOT NULL,
    colonne_modifiee VARCHAR(100),
    valeur_initiale TEXT,
    valeur_finale TEXT,
    utilisateur_id INT NULL,
    date_modification DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

