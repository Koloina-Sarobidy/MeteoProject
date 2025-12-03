DELIMITER $$

-- Fonction pour insérer dans audit_log
CREATE PROCEDURE sp_insert_audit_log(
    IN p_table_name VARCHAR(100),
    IN p_record_id INT,
    IN p_colonne VARCHAR(100),
    IN p_valeur_initiale TEXT,
    IN p_valeur_finale TEXT,
    IN p_utilisateur_id INT
)
BEGIN
    INSERT INTO audit_log(
        table_name,
        record_id,
        colonne_modifiee,
        valeur_initiale,
        valeur_finale,
        utilisateur_id
    ) VALUES (
        p_table_name,
        p_record_id,
        p_colonne,
        p_valeur_initiale,
        p_valeur_finale,
        p_utilisateur_id
    );
END$$

DELIMITER ;

-- ==========================
-- TRIGGERS POUR TABLE STATION
-- ==========================
DELIMITER $$

CREATE TRIGGER tr_station_insert
AFTER INSERT ON station
FOR EACH ROW
BEGIN
    CALL sp_insert_audit_log('station', NEW.id, 'ALL', NULL, CONCAT_WS(',', NEW.nom, NEW.latitude, NEW.longitude, NEW.type_station_id, NEW.region_id, NEW.brand_id, NEW.statut, NEW.date_debut, NEW.date_fin, NEW.est_arrete), NULL);
END$$

CREATE TRIGGER tr_station_update
AFTER UPDATE ON station
FOR EACH ROW
BEGIN
    CALL sp_insert_audit_log('station', NEW.id, 'ALL', CONCAT_WS(',', OLD.nom, OLD.latitude, OLD.longitude, OLD.type_station_id, OLD.region_id, OLD.brand_id, OLD.statut, OLD.date_debut, OLD.date_fin, OLD.est_arrete),
                                               CONCAT_WS(',', NEW.nom, NEW.latitude, NEW.longitude, NEW.type_station_id, NEW.region_id, NEW.brand_id, NEW.statut, NEW.date_debut, NEW.date_fin, NEW.est_arrete), NULL);
END$$

CREATE TRIGGER tr_station_delete
AFTER DELETE ON station
FOR EACH ROW
BEGIN
    CALL sp_insert_audit_log('station', OLD.id, 'ALL', CONCAT_WS(',', OLD.nom, OLD.latitude, OLD.longitude, OLD.type_station_id, OLD.region_id, OLD.brand_id, OLD.statut, OLD.date_debut, OLD.date_fin, OLD.est_arrete), NULL, NULL);
END$$

DELIMITER ;

-- ==========================
-- TRIGGERS POUR TABLE UTILISATEUR
-- ==========================
DELIMITER $$

CREATE TRIGGER tr_utilisateur_insert
AFTER INSERT ON utilisateur
FOR EACH ROW
BEGIN
    CALL sp_insert_audit_log('utilisateur', NEW.id, 'ALL', NULL, CONCAT_WS(',', NEW.nom, NEW.prenom, NEW.email, NEW.role_id, NEW.genre, NEW.date_debut, NEW.date_fin, NEW.crm_id, NEW.station_id, NEW.est_valide, NEW.statut), NULL);
END$$

CREATE TRIGGER tr_utilisateur_update
AFTER UPDATE ON utilisateur
FOR EACH ROW
BEGIN
    CALL sp_insert_audit_log('utilisateur', NEW.id, 'ALL', CONCAT_WS(',', OLD.nom, OLD.prenom, OLD.email, OLD.role_id, OLD.genre, OLD.date_debut, OLD.date_fin, OLD.crm_id, OLD.station_id, OLD.est_valide, OLD.statut),
                                               CONCAT_WS(',', NEW.nom, NEW.prenom, NEW.email, NEW.role_id, NEW.genre, NEW.date_debut, NEW.date_fin, NEW.crm_id, NEW.station_id, NEW.est_valide, NEW.statut), NULL);
END$$

CREATE TRIGGER tr_utilisateur_delete
AFTER DELETE ON utilisateur
FOR EACH ROW
BEGIN
    CALL sp_insert_audit_log('utilisateur', OLD.id, 'ALL', CONCAT_WS(',', OLD.nom, OLD.prenom, OLD.email, OLD.role_id, OLD.genre, OLD.date_debut, OLD.date_fin, OLD.crm_id, OLD.station_id, OLD.est_valide, OLD.statut), NULL, NULL);
END$$

DELIMITER ;

-- ==========================
-- TRIGGERS POUR TABLE CRM
-- ==========================
DELIMITER $$

CREATE TRIGGER tr_crm_insert
AFTER INSERT ON crm
FOR EACH ROW
BEGIN
    CALL sp_insert_audit_log('crm', NEW.id, 'ALL', NULL, CONCAT_WS(',', NEW.nom, NEW.date_debut, NEW.date_fin, NEW.est_arrete), NULL);
END$$

CREATE TRIGGER tr_crm_update
AFTER UPDATE ON crm
FOR EACH ROW
BEGIN
    CALL sp_insert_audit_log('crm', NEW.id, 'ALL', CONCAT_WS(',', OLD.nom, OLD.date_debut, OLD.date_fin, OLD.est_arrete),
                                         CONCAT_WS(',', NEW.nom, NEW.date_debut, NEW.date_fin, NEW.est_arrete), NULL);
END$$

CREATE TRIGGER tr_crm_delete
AFTER DELETE ON crm
FOR EACH ROW
BEGIN
    CALL sp_insert_audit_log('crm', OLD.id, 'ALL', CONCAT_WS(',', OLD.nom, OLD.date_debut, OLD.date_fin, OLD.est_arrete), NULL, NULL);
END$$

DELIMITER ;

-- ==========================
-- TRIGGERS POUR TABLE CRM_STATION
-- ==========================
DELIMITER $$

CREATE TRIGGER tr_crm_station_insert
AFTER INSERT ON crm_station
FOR EACH ROW
BEGIN
    CALL sp_insert_audit_log('crm_station', NEW.id, 'ALL', NULL, CONCAT_WS(',', NEW.crm_id, NEW.station_id), NULL);
END$$

CREATE TRIGGER tr_crm_station_update
AFTER UPDATE ON crm_station
FOR EACH ROW
BEGIN
    CALL sp_insert_audit_log('crm_station', NEW.id, 'ALL', CONCAT_WS(',', OLD.crm_id, OLD.station_id),
                                               CONCAT_WS(',', NEW.crm_id, NEW.station_id), NULL);
END$$

CREATE TRIGGER tr_crm_station_delete
AFTER DELETE ON crm_station
FOR EACH ROW
BEGIN
    CALL sp_insert_audit_log('crm_station', OLD.id, 'ALL', CONCAT_WS(',', OLD.crm_id, OLD.station_id), NULL, NULL);
END$$

DELIMITER ;
