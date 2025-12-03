DELIMITER $$

CREATE PROCEDURE nettoyer_donnees_2ans()
BEGIN
    -- 1 — Supprimer les historiques de connexion
    DELETE FROM historique_connexion
    WHERE date_heure_debut < NOW() - INTERVAL 2 YEAR;

    -- 2 — Supprimer les tâches préventives terminées > 2 ans
    DELETE FROM preventive
    WHERE date_prevue_fin < NOW() - INTERVAL 2 YEAR
       OR (date_effective_fin IS NOT NULL AND date_effective_fin < NOW() - INTERVAL 2 YEAR);

    -- 3 — Supprimer l'historique des statuts utilisateurs > 2 ans
    DELETE FROM utilisateur_statut_historique
    WHERE date_debut < NOW() - INTERVAL 2 YEAR
       AND (date_fin IS NULL OR date_fin < NOW() - INTERVAL 2 YEAR);

    -- 4 — Supprimer les tickets > 2 ans
    DELETE FROM ticket
    WHERE date_creation < NOW() - INTERVAL 2 YEAR;

    -- 5 — Supprimer les pièces jointes des tickets liés aux tickets supprimés
    DELETE tpj
    FROM ticket_piece_jointe tpj
    LEFT JOIN ticket t ON tpj.ticket_id = t.id
    WHERE t.id IS NULL;

    -- 6 — Supprimer les visibilités et vues des tickets liés aux tickets supprimés
    DELETE tv
    FROM ticket_vue tv
    LEFT JOIN ticket t ON tv.ticket_id = t.id
    WHERE t.id IS NULL;

    DELETE tvi
    FROM ticket_visibilite tvi
    LEFT JOIN ticket t ON tvi.ticket_id = t.id
    WHERE t.id IS NULL;

    -- 7 — Supprimer les codes de mot de passe expirés > 2 ans
    DELETE FROM code_mot_de_passe
    WHERE date_expiration < NOW() - INTERVAL 2 YEAR;

    -- 8 — Supprimer les inventaires > 2 ans et leurs détails
    DELETE idet
    FROM inventaire_details idet
    LEFT JOIN inventaire i ON idet.inventaire_id = i.id
    WHERE i.id IS NULL OR i.date_inventaire < NOW() - INTERVAL 2 YEAR;

    DELETE FROM inventaire
    WHERE date_inventaire < NOW() - INTERVAL 2 YEAR;

    -- 9 — Supprimer les besoins des stations > 2 ans
    DELETE FROM besoin_station
    WHERE date < NOW() - INTERVAL 2 YEAR;

    -- 10 — Supprimer les interventions > 2 ans
    DELETE FROM intervention
    WHERE date_planifiee_debut < NOW() - INTERVAL 2 YEAR
       OR (date_effective_debut IS NOT NULL AND date_effective_debut < NOW() - INTERVAL 2 YEAR);

    -- 11 — Supprimer les logs d’audit > 2 ans
    DELETE FROM audit_log
    WHERE date_modification < NOW() - INTERVAL 2 YEAR;
END$$

DELIMITER ;
