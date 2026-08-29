namespace CheckTranslation;

/// <summary>État de la vérification de mise en page pour une ligne, dans la langue affichée.</summary>
internal enum LayoutStatus
{
    /// <summary>Non analysée : source Excel, formulaire non localisable, ou ligne qui n'est pas un libellé de contrôle.</summary>
    NotChecked,

    /// <summary>Analysée : la traduction n'introduit aucun défaut.</summary>
    Ok,

    /// <summary>Contrôle à largeur fixe : le texte traduit est coupé.</summary>
    Truncated,

    /// <summary>Contrôle AutoSize : en s'élargissant, il recouvre un voisin.</summary>
    Collision,

    /// <summary>Contrôle introuvable ou géométrie incomplète : on ne peut pas conclure.</summary>
    Unverifiable,
}
