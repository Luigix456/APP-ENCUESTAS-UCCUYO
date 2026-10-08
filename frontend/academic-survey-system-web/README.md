# Academic Survey System Web

Frontend público para responder encuestas académicas desde un enlace o código QR.

## Tecnología

- React
- Vite
- TypeScript

## Flujo público

La pantalla de respuesta se abre en:

```bash
/survey/{accessCode}
```

El frontend consume la API con rutas relativas `/api/...`. En desarrollo, Vite redirige esas llamadas a `http://localhost:5047`.

## Área privada

Rutas iniciales:

```bash
/login
/app
```

La sesión autenticada usa JWT Bearer emitido por el backend. El token y su vencimiento se guardan en `sessionStorage` bajo una clave centralizada y la sesión se restaura consultando `GET /api/auth/me`.

## Comandos

```bash
npm install
npm run dev
npm run build
```

## Alcance actual

Incluye el flujo público para estudiantes y una primera base de autenticación privada con login, restauración de sesión, protección de `/app` y logout. No incluye dashboard real, gestión de sesiones, QR, usuarios, catálogo, resultados ni reportes.

## Prueba desde celulares en desarrollo

El servidor de Vite escucha en la red local (`0.0.0.0:5173`) y redirige tanto `/api` como `/hubs` al backend local `http://localhost:5047`. De esta forma, la API y SignalR pueden consumirse desde un celular a través del mismo origen del frontend.

1. Inicie el backend en `http://localhost:5047`.
2. Ejecute `npm run dev`.
3. Vite mostrará una dirección `Network`, por ejemplo `http://192.168.1.50:5173`.
4. Abra **esa dirección Network** en la PC de la encuestadora en lugar de `http://localhost:5173`.
5. El QR generado usará ese mismo origen y podrá abrirse desde celulares conectados a la misma red.

Si por algún motivo la aplicación debe abrirse con `localhost`, copie `.env.example` como `.env.local` y configure `VITE_PUBLIC_APP_URL` con la dirección LAN accesible desde los celulares.

En producción, el servidor web/reverse proxy debe enrutar `/api` y `/hubs` hacia ASP.NET Core y habilitar WebSocket para `/hubs`. El valor `VITE_PUBLIC_APP_URL` puede apuntar al dominio HTTPS institucional si se desea fijar explícitamente la URL pública de los QR.

## Identidad visual del frontend

La interfaz administrativa utiliza una adaptación visual inspirada en la identidad pública de la Universidad Católica de Cuyo: verde institucional como color de acción, acentos sobrios borgoña y dorado, superficies claras y jerarquía visual orientada a personal administrativo.

No se incluyen archivos del logotipo oficial ni se replica el diseño del sitio institucional. El componente `InstitutionBrand` usa un monograma textual neutro (`UC`) para mantener independencia de activos marcarios. Si la Universidad autoriza el uso del logotipo oficial, puede reemplazarse posteriormente por el recurso provisto por la institución.
