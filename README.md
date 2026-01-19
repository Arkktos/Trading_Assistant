# Trading Assistant - Assistant d'Analyse de Marchés

## 📋 Vue d'ensemble

Application C# locale qui analyse automatiquement les marchés financiers et envoie des notifications par email avec des suggestions d'opportunités de trading basées sur l'analyse de Claude AI.

## 🎯 Objectifs du projet

- Automatiser l'analyse quotidienne des marchés financiers
- Utiliser Claude (forfait Pro) pour générer des analyses intelligentes
- Recevoir des notifications email avec suggestions d'opportunités
- Maintenir un contrôle total sur les décisions de trading (l'application suggère, l'utilisateur décide)

## 🔧 Stack technique

- **Langage** : C# (.NET 10.0 ou supérieur)
- **Services externes** :
  - Claude Code CLI (local, inclus avec l'abonnement Claude.ai Pro) pour l'analyse
  - Alpha Vantage pour les données de marché
  - SMTP pour l'envoi d'emails ou notification windows si application lancée
- **Type d'application** : Service Windows + Interface graphique en WinUI 3 avec tray icon (IPC)

## 📦 Fonctionnalités principales

### Phase 1 - MVP (Minimum Viable Product) (Application console)
1. **Configuration initiale**
   - Paramètres de trading (capital disponible, profil de risque)
   - Liste d'actifs à surveiller (actions, indices, ETFs)
   - Configuration email (SMTP)

2. **Récupération de données toutes les 4h**
   - Prix actuels et historiques des actifs surveillés
   - Volumes de transaction
   - Variations sur différentes périodes (jour, semaine, mois)

3. **Analyse via Claude**
   - Envoi des données de marché à Claude Code
   - Prompt structuré demandant une analyse factuelle
   - Détection de patterns et opportunités potentielles

4. **Notification windows ou email**
   - Si la UI est lancée, créer une notivication Windows
   - Sinon Email avec résumé du marché
   - Liste des opportunités détectées avec contexte
   - Graphiques de prix en format texte/ASCII ou liens

### Phase 2 - Améliorations futures
- Créer l'interface et le service Windows


## Etat d'avancement

Le MVP a été créé avec succès. Son intégration dans un service Windows et l'interface WinUI on débutés mais sont toujours en cours.

---
