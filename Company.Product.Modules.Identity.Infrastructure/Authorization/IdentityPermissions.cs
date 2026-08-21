namespace Company.Product.Modules.Identity.Infrastructure.Authorization;

/// <summary>
/// Códigos de permiso del módulo Identity.
///
/// Son <c>const</c> para poder usarse dentro de <see cref="AuthorizePermissionAttribute"/>,
/// que es un atributo y solo admite constantes de compilación:
/// <code>[AuthorizePermission(IdentityPermissions.UsersRead)]</code>
///
/// Un permiso nuevo se agrega en tres sitios, en este orden:
///   1. Una constante aquí, con el formato <c>&lt;recurso&gt;.&lt;accion&gt;</c> en minúsculas.
///   2. El seed (<c>IdentityDataSeeder</c>), para que exista en BBDD y el rol Admin lo tenga.
///   3. El endpoint que lo exige, vía <see cref="AuthorizePermissionAttribute"/>.
///
/// Cada módulo define su propia clase <c>&lt;Modulo&gt;Permissions</c>; no se comparten
/// entre módulos.
/// </summary>
public static class IdentityPermissions
{
    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";
    public const string RolesRead = "roles.read";
    public const string RolesWrite = "roles.write";
    public const string PermissionsRead = "permissions.read";
    public const string PermissionsWrite = "permissions.write";

    /// <summary>
    /// Todos los permisos del módulo, en el orden en que se siembran.
    /// Lo consume <c>IdentityDataSeeder</c>; evita que un permiso nuevo quede
    /// declarado aquí pero ausente en la BBDD.
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        UsersRead,
        UsersWrite,
        RolesRead,
        RolesWrite,
        PermissionsRead,
        PermissionsWrite,
    ];
}
