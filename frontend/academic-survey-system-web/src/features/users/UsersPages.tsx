import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { getCareers } from '../../api/academicCatalogApi';
import { ApiClientError } from '../../api/apiClient';
import {
  activateUser,
  createUser,
  deactivateUser,
  deleteUser,
  getRoles,
  getUser,
  getUserCareers,
  getUsers,
  replaceUserCareers,
  replaceUserRoles,
  resetUserPassword,
  updateUser
} from '../../api/usersApi';
import { useAuth } from '../../auth/AuthProvider';
import { ConfirmDialog, Modal } from '../../components/ui/Modal';
import { useToast } from '../../components/ui/ToastProvider';
import type { CareerDto } from '../../types/academicCatalog';
import type { RoleDto, UserCareerDto, UserDto } from '../../types/users';
import { USER_STATUS_LABELS } from '../../types/users';
import { formatDateTime } from '../surveys/surveyUi';

type LoadState = 'loading' | 'ready' | 'error';

const READ_USERS_PERMISSION = 'identity.users.read';
const CREATE_USERS_PERMISSION = 'identity.users.create';
const UPDATE_USERS_PERMISSION = 'identity.users.update';
const ASSIGN_ROLES_PERMISSION = 'identity.users.assign_roles';
const READ_ROLES_PERMISSION = 'identity.roles.read';
const READ_CATALOG_PERMISSION = 'academic.catalog.read';
const MANAGE_CATALOG_PERMISSION = 'academic.catalog.manage';

interface UserFormState {
  firstName: string;
  lastName: string;
  email: string;
}

interface CreateUserFormState extends UserFormState {
  password: string;
  roleIds: string[];
}

export function UsersPage() {
  const auth = useAuth();
  const toast = useToast();
  const [users, setUsers] = useState<UserDto[]>([]);
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [state, setState] = useState<LoadState>('loading');
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [createForm, setCreateForm] = useState<CreateUserFormState>({ firstName: '', lastName: '', email: '', password: '', roleIds: [] });
  const accessToken = auth.accessToken;
  const canReadUsers = canReadIdentityUsers(auth.hasPermission);
  const canCreateUsers = canCreateIdentityUsers(auth.hasPermission);
  const canReadRoles = canReadIdentityRoles(auth.hasPermission);

  useEffect(() => {
    if (!accessToken || !canReadUsers) {
      setState('ready');
      return;
    }

    const abortController = new AbortController();

    setState('loading');
    setError(null);

    const usersRequest = getUsers({
      accessToken,
      onUnauthorized: auth.logout,
      includeInactive: true,
      signal: abortController.signal
    });
    const rolesRequest = canReadRoles
      ? getRoles({ accessToken, onUnauthorized: auth.logout, signal: abortController.signal })
      : Promise.resolve<RoleDto[]>([]);

    Promise.all([usersRequest, rolesRequest])
      .then(([nextUsers, nextRoles]) => {
        setUsers(nextUsers);
        setRoles(nextRoles);
        setState('ready');
      })
      .catch((loadError) => {
        if (abortController.signal.aborted) {
          return;
        }

        setError(getFriendlyUserError(loadError, 'No fue posible cargar los usuarios.'));
        setState('error');
      });

    return () => {
      abortController.abort();
    };
  }, [accessToken, auth.logout, canReadRoles, canReadUsers]);

  const filteredUsers = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();

    return users
      .filter((user) => (statusFilter ? user.status === statusFilter : true))
      .filter((user) => (roleFilter ? user.roles.some((role) => role.id === roleFilter || role.code === roleFilter) : true))
      .filter((user) => {
        if (!normalizedSearch) {
          return true;
        }

        return `${user.firstName} ${user.lastName} ${user.email}`.toLowerCase().includes(normalizedSearch);
      })
      .sort((left, right) => `${left.lastName} ${left.firstName}`.localeCompare(`${right.lastName} ${right.firstName}`));
  }, [roleFilter, search, statusFilter, users]);

  if (!canReadUsers) {
    return <UsersPermissionPanel />;
  }

  async function handleCreateUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!accessToken) return;
    const validationError = validateCreateUserForm(createForm);
    if (validationError) { setCreateError(validationError); return; }
    setIsCreating(true); setCreateError(null);
    try {
      const created = await createUser({
        firstName: createForm.firstName.trim(),
        lastName: createForm.lastName.trim(),
        email: createForm.email.trim(),
        password: createForm.password,
        roleIds: createForm.roleIds
      }, { accessToken, onUnauthorized: auth.logout });
      setCreateForm({ firstName: '', lastName: '', email: '', password: '', roleIds: [] });
      setShowCreateModal(false);
      const nextUsers = await getUsers({ accessToken, onUnauthorized: auth.logout, includeInactive: true });
      setUsers(nextUsers);
      toast.success('Usuario creado', `${created.firstName} ${created.lastName} ya puede acceder al sistema.`);
    } catch (submitError) {
      const message = getFriendlyUserError(submitError, 'No fue posible crear el usuario.');
      setCreateError(message); toast.error('No se pudo crear el usuario', message);
    } finally { setIsCreating(false); }
  }

  return (
    <section className="app-content users-page">
      <header className="surveys-header">
        <div>
          <p className="eyebrow">Identidad</p>
          <h2>Usuarios</h2>
          <p>Administrá cuentas, roles, carreras asociadas y estado operativo.</p>
        </div>
        {canCreateUsers ? (
          <button className="primary-button" onClick={() => setShowCreateModal(true)} type="button">Nuevo usuario</button>
        ) : null}
      </header>

      <div className="users-filters" aria-label="Filtros de usuarios">
        <label>
          <span>Buscar</span>
          <input
            className="text-input"
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Nombre, apellido o email"
            type="search"
            value={search}
          />
        </label>
        <label>
          <span>Estado</span>
          <select className="text-input" onChange={(event) => setStatusFilter(event.target.value)} value={statusFilter}>
            <option value="">Todos</option>
            {Array.from(new Set(users.map((user) => user.status))).map((status) => (
              <option key={status} value={status}>
                {formatUserStatus(status)}
              </option>
            ))}
          </select>
        </label>
        <label>
          <span>Rol</span>
          <select className="text-input" onChange={(event) => setRoleFilter(event.target.value)} value={roleFilter}>
            <option value="">Todos</option>
            {roles.map((role) => (
              <option key={role.id} value={role.id}>
                {role.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      {state === 'loading' ? <p aria-live="polite">Cargando usuarios...</p> : null}

      {state === 'error' ? (
        <div className="empty-detail" role="alert">
          <h3>No pudimos cargar usuarios</h3>
          <p>{error}</p>
        </div>
      ) : null}

      {state === 'ready' && filteredUsers.length === 0 ? (
        <div className="empty-detail">
          <h3>No hay usuarios para mostrar</h3>
          <p>Ajustá los filtros o creá una nueva cuenta.</p>
        </div>
      ) : null}

      {state === 'ready' && filteredUsers.length > 0 ? (
        <div className="users-table" role="list">
          {filteredUsers.map((user) => (
            <Link className="user-row" key={user.id} role="listitem" to={`/app/users/${user.id}`}>
              <div className="avatar-token" aria-hidden="true">
                {getInitials(user)}
              </div>
              <div>
                <strong>{formatUserName(user)}</strong>
                <span>{user.email}</span>
              </div>
              <div className="user-row__roles">
                {formatUserRoles(user)}
              </div>
              <span className={`status-badge ${user.status === 'Active' ? 'status-badge--open' : 'status-badge--closed'}`}>
                {formatUserStatus(user.status)}
              </span>
              <small>{formatDateTime(user.createdAtUtc)}</small>
            </Link>
          ))}
        </div>
      ) : null}

      <Modal open={showCreateModal} onClose={() => { if (!isCreating) { setShowCreateModal(false); setCreateError(null); } }} closeDisabled={isCreating} title="Nuevo usuario" description="Creá la cuenta, definí la contraseña inicial y asigná al menos un rol.">
        <form className="modal-form" noValidate onSubmit={handleCreateUser}>
          <UserProfileFields form={createForm} onChange={(nextForm) => setCreateForm((current) => ({ ...current, ...nextForm }))} />
          <label><span>Contraseña inicial</span><input autoComplete="new-password" className="text-input" onChange={(event) => setCreateForm((current) => ({ ...current, password: event.target.value }))} required type="password" value={createForm.password} /><small>12 a 64 caracteres, con mayúscula, minúscula, número y símbolo.</small></label>
          <RoleSelector disabled={!canReadRoles || isCreating} roles={roles} selectedRoleIds={createForm.roleIds} onChange={(roleIds) => setCreateForm((current) => ({ ...current, roleIds }))} />
          {createError ? <p className="submit-error" role="alert">{createError}</p> : null}
          <div className="modal-footer-actions"><button className="secondary-button" disabled={isCreating} onClick={() => setShowCreateModal(false)} type="button">Cancelar</button><button className="primary-button" disabled={isCreating} type="submit">{isCreating ? 'Creando...' : 'Crear usuario'}</button></div>
        </form>
      </Modal>
    </section>
  );
}

export function UserCreatePage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [state, setState] = useState<LoadState>('loading');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [form, setForm] = useState<CreateUserFormState>({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    roleIds: []
  });
  const accessToken = auth.accessToken;
  const canCreateUsers = canCreateIdentityUsers(auth.hasPermission);
  const canReadRoles = canReadIdentityRoles(auth.hasPermission);

  useEffect(() => {
    if (!accessToken || !canCreateUsers || !canReadRoles) {
      setState('ready');
      return;
    }

    const abortController = new AbortController();

    getRoles({ accessToken, onUnauthorized: auth.logout, signal: abortController.signal })
      .then((nextRoles) => {
        setRoles(nextRoles);
        setState('ready');
      })
      .catch((loadError) => {
        if (abortController.signal.aborted) {
          return;
        }

        setError(getFriendlyUserError(loadError, 'No fue posible cargar los roles.'));
        setState('error');
      });

    return () => {
      abortController.abort();
    };
  }, [accessToken, auth.logout, canCreateUsers, canReadRoles]);

  if (!canCreateUsers) {
    return <UsersPermissionPanel action="crear usuarios" />;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken) {
      return;
    }

    const validationError = validateCreateUserForm(form);

    if (validationError) {
      setError(validationError);
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      const createdUser = await createUser(
        {
          firstName: form.firstName.trim(),
          lastName: form.lastName.trim(),
          email: form.email.trim(),
          password: form.password,
          roleIds: form.roleIds
        },
        { accessToken, onUnauthorized: auth.logout }
      );
      navigate(`/app/users/${createdUser.id}`, { replace: true });
    } catch (submitError) {
      setError(getFriendlyUserError(submitError, 'No fue posible crear el usuario.'));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <section className="app-content users-page">
      <header className="surveys-header">
        <div>
          <p className="eyebrow">Identidad</p>
          <h2>Nuevo usuario</h2>
          <p>Creá una cuenta administrativa sin exponer credenciales en pantalla.</p>
        </div>
      </header>

      {state === 'loading' ? <p aria-live="polite">Cargando roles...</p> : null}
      {state === 'error' ? <p className="submit-error" role="alert">{error}</p> : null}

      <form className="survey-admin-form user-form" noValidate onSubmit={handleSubmit}>
        <UserProfileFields form={form} onChange={(nextForm) => setForm({ ...form, ...nextForm })} />
        <label>
          <span>Contraseña inicial</span>
          <input
            className="text-input"
            onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))}
            required
            type="password"
            value={form.password}
          />
        </label>
        <RoleSelector
          disabled={!canReadRoles}
          roles={roles}
          selectedRoleIds={form.roleIds}
          onChange={(roleIds) => setForm((current) => ({ ...current, roleIds }))}
        />
        {error && state !== 'error' ? <p className="submit-error" role="alert">{error}</p> : null}
        <div className="form-actions">
          <button className="primary-button" disabled={isSubmitting} type="submit">
            {isSubmitting ? 'Creando...' : 'Crear usuario'}
          </button>
          <button className="secondary-button" onClick={() => navigate('/app/users')} type="button">
            Cancelar
          </button>
        </div>
      </form>
    </section>
  );
}

export function UserDetailPage() {
  const { userId } = useParams();
  const auth = useAuth();
  const toast = useToast();
  const navigate = useNavigate();
  const [user, setUser] = useState<UserDto | null>(null);
  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [careers, setCareers] = useState<CareerDto[]>([]);
  const [userCareers, setUserCareers] = useState<UserCareerDto[]>([]);
  const [profileForm, setProfileForm] = useState<UserFormState>({ firstName: '', lastName: '', email: '' });
  const [roleIds, setRoleIds] = useState<string[]>([]);
  const [careerIds, setCareerIds] = useState<string[]>([]);
  const [password, setPassword] = useState('');
  const [showPasswordModal, setShowPasswordModal] = useState(false);
  const [editSection, setEditSection] = useState<'profile' | 'roles' | 'careers' | null>(null);
  const [confirmAction, setConfirmAction] = useState<'status' | 'delete' | null>(null);
  const [state, setState] = useState<LoadState>('loading');
  const [error, setError] = useState<string | null>(null);
  const [activeAction, setActiveAction] = useState<string | null>(null);
  const accessToken = auth.accessToken;
  const canReadUsers = canReadIdentityUsers(auth.hasPermission);
  const canUpdateUsers = canUpdateIdentityUsers(auth.hasPermission);
  const canDeleteUsers = canDeleteIdentityUsers(auth.hasPermission);
  const canAssignRoles = canAssignIdentityRoles(auth.hasPermission);
  const canReadRoles = canReadIdentityRoles(auth.hasPermission);
  const canReadCatalog = auth.hasPermission(READ_CATALOG_PERMISSION) || auth.hasPermission(MANAGE_CATALOG_PERMISSION);
  const isSelf = user?.id === auth.user?.id;

  useEffect(() => {
    if (!accessToken || !canReadUsers || !userId) {
      setState('ready');
      return;
    }

    void loadUserDetail(accessToken, userId);
  }, [accessToken, canReadUsers, userId]);

  if (!canReadUsers) {
    return <UsersPermissionPanel />;
  }

  async function loadUserDetail(token: string, id: string) {
    setState('loading');
    setError(null);

    try {
      const requests: [
        Promise<UserDto>,
        Promise<RoleDto[]>,
        Promise<UserCareerDto[]>,
        Promise<CareerDto[]>
      ] = [
        getUser(id, { accessToken: token, onUnauthorized: auth.logout }),
        canReadRoles ? getRoles({ accessToken: token, onUnauthorized: auth.logout }) : Promise.resolve([]),
        getUserCareers(id, { accessToken: token, onUnauthorized: auth.logout }),
        canReadCatalog ? getCareers({ accessToken: token, onUnauthorized: auth.logout, includeInactive: true }) : Promise.resolve([])
      ];

      const [nextUser, nextRoles, nextUserCareers, nextCareers] = await Promise.all(requests);
      setUser(nextUser);
      setRoles(nextRoles);
      setUserCareers(nextUserCareers);
      setCareers(nextCareers);
      setProfileForm({
        firstName: nextUser.firstName,
        lastName: nextUser.lastName,
        email: nextUser.email
      });
      setRoleIds(nextUser.roles.map((role) => role.id));
      setCareerIds(nextUserCareers.map((career) => career.careerId));
      setState('ready');
    } catch (loadError) {
      setError(getFriendlyUserError(loadError, 'No fue posible cargar el usuario.'));
      setState('error');
    }
  }

  async function runAction(action: string, success: string, fallback: string, callback: () => Promise<unknown>) {
    setActiveAction(action);
    setError(null);

    try {
      await callback();
      if (accessToken && userId) await loadUserDetail(accessToken, userId);
      toast.success(success);
      return true;
    } catch (actionError) {
      const friendly = getFriendlyUserError(actionError, fallback);
      toast.error('No se pudo completar la acción', friendly);
      return false;
    } finally {
      setActiveAction(null);
    }
  }

  async function handleSaveProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || !user) {
      return;
    }

    const validationError = validateUserForm(profileForm);

    if (validationError) {
      setError(validationError);
      return;
    }

    const succeeded = await runAction('profile', 'Perfil actualizado.', 'No fue posible actualizar el perfil.', () =>
      updateUser(
        user.id,
        {
          firstName: profileForm.firstName.trim(),
          lastName: profileForm.lastName.trim(),
          email: profileForm.email.trim()
        },
        { accessToken, onUnauthorized: auth.logout }
      )
    );
    if (succeeded) setEditSection(null);
  }

  async function handleSaveRoles(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || !user) {
      return;
    }

    const succeeded = await runAction('roles', 'Roles actualizados.', 'No fue posible actualizar los roles.', () =>
      replaceUserRoles(user.id, { roleIds }, { accessToken, onUnauthorized: auth.logout })
    );
    if (succeeded) setEditSection(null);
  }

  async function handleSaveCareers(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || !user) {
      return;
    }

    const succeeded = await runAction('careers', 'Carreras asociadas actualizadas.', 'No fue posible actualizar las carreras.', () =>
      replaceUserCareers(user.id, { careerIds }, { accessToken, onUnauthorized: auth.logout })
    );
    if (succeeded) setEditSection(null);
  }

  async function handlePasswordReset(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || !user) {
      return;
    }

    const passwordError = validatePassword(password);

    if (passwordError) {
      setError(passwordError);
      return;
    }

    const succeeded = await runAction('password', 'Contraseña actualizada.', 'No fue posible actualizar la contraseña.', () =>
      resetUserPassword(user.id, { newPassword: password }, { accessToken, onUnauthorized: auth.logout })
    );
    if (succeeded) {
      setPassword('');
      setShowPasswordModal(false);
    }
  }

  async function handleToggleStatus() {
    if (!accessToken || !user || isSelf) {
      return;
    }

    await runAction(
      'status',
      user.status === 'Active' ? 'Usuario desactivado.' : 'Usuario activado.',
      user.status === 'Active' ? 'No fue posible desactivar el usuario.' : 'No fue posible activar el usuario.',
      () =>
        user.status === 'Active'
          ? deactivateUser(user.id, { accessToken, onUnauthorized: auth.logout })
          : activateUser(user.id, { accessToken, onUnauthorized: auth.logout })
    );
  }

  async function handleDelete() {
    if (!accessToken || !user || isSelf) {
      return;
    }

    await runAction('delete', 'Usuario dado de baja.', 'No fue posible dar de baja el usuario.', () =>
      deleteUser(user.id, { accessToken, onUnauthorized: auth.logout })
    );
    setConfirmAction(null);
  }

  if (state === 'loading') {
    return (
      <section className="app-content">
        <p>Cargando usuario...</p>
      </section>
    );
  }

  if (state === 'error' || !user) {
    return (
      <section className="app-content" role="alert">
        <p className="eyebrow">Identidad</p>
        <h2>No pudimos cargar el usuario</h2>
        <p>{error ?? 'El usuario solicitado no está disponible.'}</p>
      </section>
    );
  }

  return (
    <section className="app-content users-page user-detail-page">
      <header className="surveys-header">
        <div>
          <p className="eyebrow">Identidad</p>
          <h2>{formatUserName(user)}</h2>
          <p>{user.email}</p>
        </div>
        <div className="badge-group">
          <span className={`status-badge ${user.status === 'Active' ? 'status-badge--open' : 'status-badge--closed'}`}>
            {formatUserStatus(user.status)}
          </span>
        </div>
      </header>

      <div className="user-detail-grid">
        <section className="crud-summary-card">
          <header><div><p className="eyebrow">Perfil</p><h3>Datos personales</h3></div><button className="secondary-button" disabled={!canUpdateUsers} onClick={() => setEditSection('profile')} type="button">Editar</button></header>
          <dl className="summary-list"><div><dt>Nombre</dt><dd>{formatUserName(user)}</dd></div><div><dt>Email</dt><dd>{user.email}</dd></div></dl>
        </section>

        <section className="crud-summary-card">
          <header><div><p className="eyebrow">Acceso</p><h3>Roles</h3></div><button className="secondary-button" disabled={!canAssignRoles || !canReadRoles} onClick={() => setEditSection('roles')} type="button">Editar</button></header>
          <div className="tag-list">{user.roles.length ? user.roles.map((role) => <span className="soft-tag" key={role.id}>{role.name}</span>) : <span className="muted-text">Sin roles asignados</span>}</div>
        </section>

        <section className="crud-summary-card">
          <header><div><p className="eyebrow">Alcance</p><h3>Carreras asociadas</h3></div><button className="secondary-button" disabled={!canReadCatalog || !canUpdateUsers} onClick={() => setEditSection('careers')} type="button">Editar</button></header>
          <div className="tag-list">{userCareers.length ? userCareers.map((career) => <span className="soft-tag" key={career.careerId}>{career.careerName}</span>) : <span className="muted-text">Sin carreras asociadas</span>}</div>
        </section>

        <section className="crud-summary-card">
          <header><div><p className="eyebrow">Seguridad</p><h3>Cuenta y estado</h3></div></header>
          <dl className="user-security-meta"><div><dt>Creado</dt><dd>{formatDateTime(user.createdAtUtc)}</dd></div><div><dt>Actualizado</dt><dd>{formatDateTime(user.updatedAtUtc)}</dd></div></dl>
          <div className="form-actions">
            <button className="secondary-button" disabled={!canUpdateUsers || activeAction !== null} onClick={() => setShowPasswordModal(true)} type="button">Restablecer contraseña</button>
            <button className="secondary-button" disabled={!canUpdateUsers || isSelf || activeAction !== null} onClick={() => setConfirmAction('status')} type="button">{user.status === 'Active' ? 'Desactivar' : 'Activar'}</button>
            <button className="danger-button" disabled={!canDeleteUsers || isSelf || activeAction !== null} onClick={() => setConfirmAction('delete')} type="button">Dar de baja</button>
          </div>
          {isSelf ? <p className="inline-message">No podés desactivar ni dar de baja tu propia cuenta desde esta pantalla.</p> : null}
        </section>
      </div>

      <div className="form-actions"><button className="secondary-button" onClick={() => navigate('/app/users')} type="button">Volver a usuarios</button></div>

      <Modal open={editSection === 'profile'} onClose={() => setEditSection(null)} title="Editar datos personales" description="Actualizá nombre, apellido o correo del usuario.">
        <form className="modal-form" noValidate onSubmit={handleSaveProfile}><UserProfileFields form={profileForm} onChange={setProfileForm} />{error ? <p className="submit-error" role="alert">{error}</p> : null}<div className="modal-footer-actions"><button className="secondary-button" onClick={() => setEditSection(null)} type="button">Cancelar</button><button className="primary-button" disabled={activeAction !== null} type="submit">{activeAction === 'profile' ? 'Guardando...' : 'Guardar cambios'}</button></div></form>
      </Modal>

      <Modal open={editSection === 'roles'} onClose={() => setEditSection(null)} title="Editar roles" description="Los roles determinan qué módulos y acciones puede utilizar esta cuenta.">
        <form className="modal-form" noValidate onSubmit={handleSaveRoles}><RoleSelector disabled={!canAssignRoles || !canReadRoles || activeAction !== null} roles={roles} selectedRoleIds={roleIds} onChange={setRoleIds} /><div className="modal-footer-actions"><button className="secondary-button" onClick={() => setEditSection(null)} type="button">Cancelar</button><button className="primary-button" disabled={!canAssignRoles || activeAction !== null} type="submit">{activeAction === 'roles' ? 'Guardando...' : 'Guardar roles'}</button></div></form>
      </Modal>

      <Modal open={editSection === 'careers'} onClose={() => setEditSection(null)} title="Editar carreras asociadas" description="Estas carreras delimitan el alcance de usuarios como Director/a de carrera.">
        <form className="modal-form" noValidate onSubmit={handleSaveCareers}>{canReadCatalog ? <CareerSelector careers={careers} disabled={!canUpdateUsers || activeAction !== null} selectedCareerIds={careerIds} onChange={setCareerIds} /> : <p className="inline-message">No tenés permiso de lectura del catálogo para modificar carreras.</p>}<div className="modal-footer-actions"><button className="secondary-button" onClick={() => setEditSection(null)} type="button">Cancelar</button><button className="primary-button" disabled={!canReadCatalog || !canUpdateUsers || activeAction !== null} type="submit">{activeAction === 'careers' ? 'Guardando...' : 'Guardar carreras'}</button></div></form>
      </Modal>

      <Modal open={showPasswordModal} onClose={() => { if (activeAction === null) { setPassword(''); setShowPasswordModal(false); } }} closeDisabled={activeAction !== null} title="Restablecer contraseña" description="Ingresá una contraseña nueva. La contraseña actual nunca se muestra en pantalla.">
        <form className="modal-form" noValidate onSubmit={handlePasswordReset}><label><span>Nueva contraseña</span><input autoComplete="new-password" className="text-input" onChange={(event) => setPassword(event.target.value)} required type="password" value={password} /><small>12 a 64 caracteres, con mayúscula, minúscula, número y símbolo.</small></label>{error ? <p className="submit-error" role="alert">{error}</p> : null}<div className="modal-footer-actions"><button className="secondary-button" disabled={activeAction !== null} onClick={() => { setPassword(''); setShowPasswordModal(false); }} type="button">Cancelar</button><button className="primary-button" disabled={activeAction !== null} type="submit">{activeAction === 'password' ? 'Guardando...' : 'Guardar contraseña'}</button></div></form>
      </Modal>

      <ConfirmDialog
        busy={activeAction === 'status'}
        confirmLabel={user.status === 'Active' ? 'Desactivar usuario' : 'Activar usuario'}
        message={user.status === 'Active' ? 'El usuario perderá acceso al sistema hasta que vuelva a activarse. Su historial se conservará.' : 'El usuario recuperará el acceso de acuerdo con sus roles y permisos.'}
        onCancel={() => setConfirmAction(null)}
        onConfirm={() => { void handleToggleStatus().finally(() => setConfirmAction(null)); }}
        open={confirmAction === 'status'}
        title={user.status === 'Active' ? '¿Desactivar este usuario?' : '¿Activar este usuario?'}
        tone={user.status === 'Active' ? 'danger' : 'primary'}
      />
      <ConfirmDialog
        busy={activeAction === 'delete'}
        confirmLabel="Dar de baja"
        message="Esta baja es operativa: el usuario dejará de poder utilizar la aplicación, pero su historial será conservado."
        onCancel={() => setConfirmAction(null)}
        onConfirm={() => void handleDelete()}
        open={confirmAction === 'delete'}
        title="¿Dar de baja este usuario?"
        tone="danger"
      />
    </section>
  );
}

function UsersPermissionPanel({ action = 'consultar usuarios' }: { action?: string }) {
  return (
    <section className="app-content access-denied-panel">
      <p className="eyebrow">Sin acceso</p>
      <h2>No tenés permisos para {action}.</h2>
      <p>Solicitá permisos de identidad a la administración del sistema.</p>
    </section>
  );
}

function UserProfileFields({
  form,
  onChange
}: {
  form: UserFormState;
  onChange: (nextForm: UserFormState) => void;
}) {
  return (
    <>
      <label>
        <span>Nombre</span>
        <input
          className="text-input"
          maxLength={100}
          onChange={(event) => onChange({ ...form, firstName: event.target.value })}
          required
          type="text"
          value={form.firstName}
        />
      </label>
      <label>
        <span>Apellido</span>
        <input
          className="text-input"
          maxLength={100}
          onChange={(event) => onChange({ ...form, lastName: event.target.value })}
          required
          type="text"
          value={form.lastName}
        />
      </label>
      <label>
        <span>Email</span>
        <input
          className="text-input"
          maxLength={320}
          onChange={(event) => onChange({ ...form, email: event.target.value })}
          required
          type="email"
          value={form.email}
        />
      </label>
    </>
  );
}

function RoleSelector({
  disabled,
  roles,
  selectedRoleIds,
  onChange
}: {
  disabled: boolean;
  roles: RoleDto[];
  selectedRoleIds: string[];
  onChange: (roleIds: string[]) => void;
}) {
  if (roles.length === 0) {
    return <p className="inline-message">No hay roles disponibles para seleccionar.</p>;
  }

  return (
    <fieldset className="checkbox-list" disabled={disabled}>
      <legend>Roles</legend>
      {roles.map((role) => (
        <label className="checkbox-field" key={role.id}>
          <input
            checked={selectedRoleIds.includes(role.id)}
            onChange={(event) =>
              onChange(toggleSelection(selectedRoleIds, role.id, event.target.checked))
            }
            type="checkbox"
          />
          <span>{role.name}</span>
        </label>
      ))}
    </fieldset>
  );
}

function CareerSelector({
  careers,
  disabled,
  selectedCareerIds,
  onChange
}: {
  careers: CareerDto[];
  disabled: boolean;
  selectedCareerIds: string[];
  onChange: (careerIds: string[]) => void;
}) {
  if (careers.length === 0) {
    return <p className="inline-message">No hay carreras disponibles para seleccionar.</p>;
  }

  return (
    <fieldset className="checkbox-list" disabled={disabled}>
      <legend>Carreras</legend>
      {careers.map((career) => (
        <label className="checkbox-field" key={career.id}>
          <input
            checked={selectedCareerIds.includes(career.id)}
            onChange={(event) =>
              onChange(toggleSelection(selectedCareerIds, career.id, event.target.checked))
            }
            type="checkbox"
          />
          <span>
            {career.name} · {career.code}
          </span>
        </label>
      ))}
    </fieldset>
  );
}

function toggleSelection(current: string[], id: string, selected: boolean): string[] {
  if (selected) {
    return current.includes(id) ? current : [...current, id];
  }

  return current.filter((item) => item !== id);
}

function validateCreateUserForm(form: CreateUserFormState): string | null {
  const profileError = validateUserForm(form);

  if (profileError) {
    return profileError;
  }

  return validatePassword(form.password);
}

function validateUserForm(form: UserFormState): string | null {
  if (!form.firstName.trim()) {
    return 'Ingresá el nombre.';
  }

  if (!form.lastName.trim()) {
    return 'Ingresá el apellido.';
  }

  if (!form.email.trim() || !form.email.includes('@')) {
    return 'Ingresá un email válido.';
  }

  return null;
}

function validatePassword(value: string): string | null {
  if (!value) {
    return 'Ingresá la nueva contraseña.';
  }

  if (value.length < 8) {
    return 'La contraseña debe tener al menos 8 caracteres.';
  }

  return null;
}

function getFriendlyUserError(error: unknown, fallback: string): string {
  if (error instanceof ApiClientError) {
    if (error.status === 401) {
      return 'La sesión de usuario venció. Volvé a iniciar sesión.';
    }

    if (error.status === 403) {
      return 'No tenés permisos para administrar usuarios.';
    }

    if (error.status === 404) {
      return 'El usuario solicitado no existe.';
    }

    if (error.status === 409) {
      return 'La operación no puede completarse porque afectaría reglas de seguridad del sistema.';
    }

    if (error.status === 400) {
      return getValidationMessage(error.code);
    }
  }

  return fallback;
}

function getValidationMessage(code: string | null): string {
  switch (code) {
    case 'User.FirstNameRequired':
      return 'Ingresá el nombre.';
    case 'User.LastNameRequired':
      return 'Ingresá el apellido.';
    case 'User.EmailRequired':
    case 'User.EmailInvalid':
      return 'Ingresá un email válido.';
    case 'User.PasswordRequired':
    case 'User.PasswordPolicyInvalid':
      return 'La contraseña no cumple la política de seguridad.';
    case 'User.RoleRequired':
      return 'Seleccioná al menos un rol.';
    default:
      return 'No fue posible procesar la solicitud. Revisá los datos e intentá nuevamente.';
  }
}

function formatUserName(user: Pick<UserDto, 'firstName' | 'lastName'>): string {
  return `${user.firstName} ${user.lastName}`.trim();
}

function formatUserRoles(user: UserDto): string {
  return user.roles.length > 0 ? user.roles.map((role) => role.name).join(', ') : 'Sin roles asignados';
}

function formatUserStatus(status: string): string {
  return USER_STATUS_LABELS[status] ?? status;
}

function getInitials(user: Pick<UserDto, 'firstName' | 'lastName' | 'email'>): string {
  const first = user.firstName.trim()[0];
  const last = user.lastName.trim()[0];
  return `${first ?? user.email[0] ?? 'U'}${last ?? ''}`.toUpperCase();
}

function canReadIdentityUsers(hasPermission: (permission: string) => boolean): boolean {
  return hasPermission(READ_USERS_PERMISSION);
}

function canCreateIdentityUsers(hasPermission: (permission: string) => boolean): boolean {
  return hasPermission(CREATE_USERS_PERMISSION);
}

function canUpdateIdentityUsers(hasPermission: (permission: string) => boolean): boolean {
  return hasPermission(UPDATE_USERS_PERMISSION);
}

function canDeleteIdentityUsers(hasPermission: (permission: string) => boolean): boolean {
  return canUpdateIdentityUsers(hasPermission);
}

function canAssignIdentityRoles(hasPermission: (permission: string) => boolean): boolean {
  return hasPermission(ASSIGN_ROLES_PERMISSION);
}

function canReadIdentityRoles(hasPermission: (permission: string) => boolean): boolean {
  return hasPermission(READ_ROLES_PERMISSION);
}
