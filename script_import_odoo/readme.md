# Script d'import automatique des produits en C#

Ce script C# permet d'importer automatiquement des produits, de générer leurs variantes (attributs) et de mettre à jour les niveaux de stock dans Odoo via l'interface **XML-RPC**.

## Fonctionnalités
* **Authentification sécurisée** : Connexion via l'API Odoo.
* **Gestion intelligente des attributs** : Crée automatiquement les attributs (ex: Taille, Couleur) et leurs valeurs s'ils n'existent pas.
* **Génération de variantes** : Lie les attributs aux modèles de produits (`product.template`) pour générer les déclinaisons.
* **Synchronisation des prix** : Met à jour le prix de vente et le coût au niveau de la variante spécifique.
* **Mise à jour du stock (On Hand)** : Force la quantité en stock dans un emplacement spécifique et valide l'inventaire.

---

## Prérequis
* **.NET SDK** (version 6.0 ou supérieure).
* **Odoo**
* Librairie **CookComputing.XmlRpcV2** 
* **Mode Développeur** activé sur Odoo pour récupérer les IDs d'emplacement.

---

## Structure du fichier CSV (`produits.csv`)
Le script attend un fichier CSV nommé `produits.csv` à la racine du projet, encodé en UTF-8, avec le format suivant :

| Nom | Prix | Coût | Attribut | Valeur | Stock |
| :--- | :--- | :--- | :--- | :--- | :--- |
| Ecran 4K | 450 | 300 | 27 pouces | Oui | 20 |
| T-Shirt | 25 | 10 | Couleur | Rouge | 50 |

---

## Configuration
Modifiez les constantes en haut du fichier `Program.cs` pour les adapter à votre environnement :

```csharp
const string URL = "http://votre-odoo:8069/xmlrpc/2/";
const string DB = "votre_base_de_donnees";
const string USER = "votre_email";
const string PASS = "votre_mot_de_passe";
const int MY_LOCATION_ID = 8; // ID de l'emplacement interne (Stock)

## Comment ca marche ?

* Lancez Odoo
* Placer votre fichier.csv dans le repo script_import_odoo
* dans un terminal, allez a la racine du repo script_import_odoo
* Executer la commande ´dotnet run´

Vous devez Voir le script s'executer 