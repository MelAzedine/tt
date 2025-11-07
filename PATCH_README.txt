TRIDENT.MITM — patch (vibrations / DS5 Edge USB)

Ce zip ajoute :
1) **EdgeRumbleDiagnostic.cs** : un helper autonome qui ouvre la bonne interface HID (USB) et envoie un paquet
   compatible DS4 et DualSense/Edge. Il utilise `GetMaxOutputReportLength()` pour garantir la bonne longueur
   (64 pour 0x02/DS5), ce qui corrige le cas où l’appli voyait `OutLen=64` mais envoyait un buffer de 63 octets.
2) **ToolsWindow.xaml** : `Background` corrigé (utilise maintenant la brosse `B_Bg0`) pour éviter l’erreur
   « ressource Bg0 de type incompatible ». Rien d’autre n’a été modifié dans l’UI.
3) **ToolsWindow.Rumble.cs** : branche les boutons **Pulse / Ramp 1s / Stop** sur :
   - `MainWindow.UiRumblePulse` si votre fenêtre principale expose cette méthode,
   - sinon un fallback direct HID via `EdgeRumbleDiagnostic.Pulse(...)`.

Integration rapide
------------------
- Copiez les 3 fichiers dans votre projet `Trident.MITM.App` (même dossier que `ToolsWindow.xaml`).
- Si `ToolsWindow.xaml.cs` contient déjà des handlers `Pulse_Click/Ramp_Click/Stop_Click`, commentez-les
  ou laissez ceux de ce patch : ils sont dans une `partial class` séparée.
- **Aucun changement dans App.xaml requis**. `ToolsWindow.xaml` référence *Bg0/Bg1/Fg/Sub* à travers les
  brosses locales `B_Bg0`, `B_Bg1`, etc.

Test rapide
-----------
- Lancer l’app, ouvrir **Outils**, régler les sliders Small/Large/Durée puis **Pulse**.
- En USB DualSense/Edge, le log HID devrait montrer `Probe HID: VID=0x054C PID=... OutLen=64` et la manette doit vibrer.
- Si vous êtes en DS4 (05C4/09CC/0BA0), le test envoie le report 0x05 standard.

Notes techniques
----------------
- Le helper préfère **USB** (les endpoints BT ne reçoivent pas ces reports). 
- Le buffer est alloué avec `Math.Max(ExpectedLen, dev.GetMaxOutputReportLength())` afin de s’aligner sur
  les drivers qui requièrent la longueur exacte (Windows HID).
- Pour Edge/DS5, le paquet simple est: `0x02 0x01 ... [large]=b[3], [small]=b[4], b[10]=0x02`. Il est ré-envoyé
  pendant ~300ms pour maintenir l’activation, puis un paquet à 0 coupe les moteurs.

