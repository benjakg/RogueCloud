using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;

public class LogicaPolicia1 : MonoBehaviour
{
    public float velocidadMovimiento = 3.0f; // velocidad de patrulla
    public float velocidadRotacion = 200.0f;
    private Animator anim;

    // Patrulla entre dos puntos específicos
    public Vector3 puntoA = new Vector3(-11.24f, 0.04223061f, 7.29506f);
    public Vector3 puntoB = new Vector3(-1.79f, 0.04223061f, 7.29506f);
    public float umbralLlegada = 0.2f;
    public float esperaEnPuntoSeg = 0.0f;

    private float temporizadorEspera;
    private bool yendoHaciaB = true; // true = yendo hacia B, false = yendo hacia A

    // Estado de destino actual
    private Vector3 destinoActual;
    private bool tieneDestino = false;
    private Vector3 ultimaPosicion;
    private float tiempoSinProgreso = 0f;
    public float tiempoMaxSinProgreso = 3f; // si se queda casi quieto, cambia de destino

    // Persecución al jugador
    public Transform jugador;
    public string jugadorTag = "Player";
    public float radioDeteccion = 15f; // Aumentado para mayor rango de visión
    public float radioPerdida = 20f; // Aumentado proporcionalmente
    public float velocidadPersecucion = 4.5f;
    private bool persiguiendo = false;
    public string escenaGameOver = ""; // opcional: asigna el nombre de la escena de Game Over
    
    // Campo de visión para detectar solo si el jugador está delante
    [Range(0f, 180f)]
    public float anguloVision = 60f; // Ángulo de visión en grados (delante del policía)
    
    // Sistema de visión del jugador
    public float tiempoSinVerJugador = 2f; // Segundos sin ver al jugador antes de dejar de perseguir
    private float tiempoUltimaVistaJugador = 0f; // Cuándo vio al jugador por última vez
    public LayerMask capaVision = -1; // Capas que bloquean la visión (default: todas)
    
    // Detección de obstáculos
    public float distanciaDeteccionObstaculos = 2f; // Distancia a la que detecta obstáculos antes de chocar
    public LayerMask capaObstaculos = -1; // Capas a considerar como obstáculos (default: todas)
    private float tiempoUltimoChoque = 0f; // Para evitar cambios repetidos de dirección
    public float tiempoEntreCambiosDireccion = 1f; // Tiempo mínimo entre cambios de dirección
    private int contadorCambiosDireccion = 0; // Para evitar bucles infinitos
    private const int maxCambiosDireccion = 3; // Máximo de cambios antes de ignorar obstáculos temporalmente
    private Collider colliderPropio; // Collider del policía para ignorarlo
    
    // Sistema de captura del jugador
    public float tiempoCaptura = 1f; // Tiempo en segundos que debe estar en contacto para capturar
    private float tiempoCapturando = 0f; // Tiempo acumulado de contacto
    private bool capturando = false; // Si está capturando al jugador
    private bool videoReproduciendo = false; // Si el video está reproduciéndose
    
    // Video de captura
    public VideoClip videoCaptura; // Video a reproducir cuando capture al jugador
    public string nombreVideo = "AnimacionCaptura"; // Nombre del video en Resources
    public string nombreObjetoVideo = "VideoAtrapada"; // Nombre del objeto en la jerarquía
    private VideoPlayer videoPlayer;
    private RawImage videoDisplay; // UI para mostrar el video
    private Canvas videoCanvas; // Canvas para el video
    private GameObject objetoVideo; // Objeto VideoAtrapada de la jerarquía

    void Start()
    {
        anim = GetComponent<Animator>();
        
        // Obtener el collider propio para ignorarlo en los raycasts
        colliderPropio = GetComponent<Collider>();
        if(colliderPropio == null)
        {
            colliderPropio = GetComponentInChildren<Collider>();
        }

        // Buscar jugador si no está asignado
        if(jugador == null && !string.IsNullOrEmpty(jugadorTag))
        {
            GameObject goPlayer = GameObject.FindGameObjectWithTag(jugadorTag);
            if(goPlayer != null)
            {
                jugador = goPlayer.transform;
            }
        }

        // Inicializar patrulla hacia el punto B desde el punto A
        yendoHaciaB = true;
        destinoActual = puntoB;
        tieneDestino = true;
        temporizadorEspera = 0f;
        ultimaPosicion = transform.position;
        contadorCambiosDireccion = 0;
        tiempoUltimaVistaJugador = 0f;
        tiempoCapturando = 0f;
        capturando = false;
        videoReproduciendo = false;
        
        // Configurar el sistema de video
        ConfigurarVideoCaptura();
    }

    void Update()
    {
        // Sistema de captura mejorado: verificar continuamente si el jugador está cerca
        if(jugador != null && !videoReproduciendo && !capturando)
        {
            float distancia = Vector3.Distance(transform.position, jugador.position);
            // Si el jugador está muy cerca, iniciar captura automáticamente
            if(distancia <= 1.5f)
            {
                // Verificar que realmente estén en contacto usando raycast
                Vector3 direccion = (jugador.position - transform.position).normalized;
                RaycastHit hit;
                if(Physics.Raycast(transform.position + Vector3.up * 0.5f, direccion, out hit, 1.5f))
                {
                    if(hit.collider != null && (hit.collider.CompareTag(jugadorTag) || hit.collider.transform == jugador || hit.collider.transform.IsChildOf(jugador)))
                    {
                        capturando = true;
                        tiempoCapturando = 0f;
                        Debug.Log("¡Captura detectada por Update! Jugador muy cerca. Distancia: " + distancia.ToString("F2"));
                    }
                }
            }
        }
        
        // Continuar conteo si ya está capturando
        if(capturando && jugador != null && !videoReproduciendo)
        {
            float distancia = Vector3.Distance(transform.position, jugador.position);
            if(distancia <= 2.0f)
            {
                tiempoCapturando += Time.deltaTime;
                
                // Si ha capturado al jugador durante el tiempo suficiente, mostrar video
                if(tiempoCapturando >= tiempoCaptura)
                {
                    Debug.Log("¡TIEMPO COMPLETADO (Update)! Llamando a ReproducirVideoCaptura()");
                    ReproducirVideoCaptura();
                    capturando = false;
                    tiempoCapturando = 0f;
                }
            }
            else if(distancia > 3.0f)
            {
                // Solo resetear si se aleja mucho
                Debug.Log("Jugador se alejó mucho (Update), reseteando captura. Distancia: " + distancia.ToString("F2"));
                capturando = false;
                tiempoCapturando = 0f;
            }
        }
        
        // Chequear detección de jugador mientras NO esté reproduciendo el video
        if(jugador != null && !videoReproduciendo)
        {
            float distJugador = Vector3.Distance(transform.position, jugador.position);
            
            // Verificar si realmente puede VER al jugador (sin obstáculos en medio)
            bool puedeVerJugador = PuedeVerJugador();
            
            if(!persiguiendo)
            {
                // Empezar a perseguir si el jugador está en el rango de detección y puede verlo
                if(distJugador <= radioDeteccion && puedeVerJugador)
                {
                    persiguiendo = true;
                    tiempoUltimaVistaJugador = Time.time;
                }
            }
            else
            {
                // Si está persiguiendo, verificar si todavía puede ver al jugador y está en rango
                if(puedeVerJugador && distJugador <= radioPerdida)
                {
                    // Actualizar tiempo de última vista - sigue persiguiendo
                    tiempoUltimaVistaJugador = Time.time;
                }
                else
                {
                    // No puede ver al jugador o está fuera de rango
                    // Verificar si ha pasado el tiempo límite sin verlo
                    float tiempoSinVer = Time.time - tiempoUltimaVistaJugador;
                    
                    if(tiempoSinVer >= tiempoSinVerJugador || distJugador >= radioPerdida)
                    {
                        // Dejar de perseguir después del tiempo sin ver o si se aleja mucho
                        persiguiendo = false;
                        tiempoUltimaVistaJugador = 0f;
                        // Volver a la patrulla entre los puntos A y B
                        ReanudarPatrulla();
                    }
                }
            }
        }

        if(persiguiendo && jugador != null && !videoReproduciendo)
        {
            // Movimiento de persecución
            Vector3 haciaJugador = jugador.position - transform.position;
            Vector3 direccionPlano = Vector3.ProjectOnPlane(haciaJugador, Vector3.up);
            Vector3 direccion = direccionPlano.normalized;
            
            // Verificar obstáculos antes de moverse (incluso persiguiendo, pero con menos sensibilidad)
            if(contadorCambiosDireccion < maxCambiosDireccion && DetectarObstaculo(direccion, true))
            {
                CambiarDireccion();
                return;
            }
            
            transform.position += direccion * velocidadPersecucion * Time.deltaTime;
            
            // Reducir contador si se está moviendo bien
            if(Vector3.Distance(transform.position, ultimaPosicion) > 0.1f)
            {
                contadorCambiosDireccion = Mathf.Max(0, contadorCambiosDireccion - 1);
            }

            if(direccion.sqrMagnitude > 0.0001f)
            {
                Quaternion rotObjetivo = Quaternion.LookRotation(direccion, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObjetivo, velocidadRotacion * Time.deltaTime);
            }

            if(anim != null)
            {
                anim.SetFloat("VelX", 0f);
                anim.SetFloat("VelY", velocidadPersecucion);
            }

            ultimaPosicion = transform.position;
            return;
        }

        // Movimiento de patrulla entre puntos A y B
        if(tieneDestino && !videoReproduciendo)
        {
            if(temporizadorEspera > 0f)
            {
                temporizadorEspera -= Time.deltaTime;
                if(anim != null)
                {
                    anim.SetFloat("VelX", 0f);
                    anim.SetFloat("VelY", 0f);
                }
                return;
            }

            Vector3 haciaObjetivo = destinoActual - transform.position;
            Vector3 direccionPlano = Vector3.ProjectOnPlane(haciaObjetivo, Vector3.up);
            float distancia = direccionPlano.magnitude;

            if(distancia <= umbralLlegada)
            {
                // Cambiar al siguiente punto de patrulla (A o B)
                if(yendoHaciaB)
                {
                    destinoActual = puntoA;
                    yendoHaciaB = false;
                }
                else
                {
                    destinoActual = puntoB;
                    yendoHaciaB = true;
                }
                temporizadorEspera = esperaEnPuntoSeg;
                if(anim != null)
                {
                    anim.SetFloat("VelX", 0f);
                    anim.SetFloat("VelY", 0f);
                }
                return;
            }

            Vector3 direccion = direccionPlano.normalized;
            
            // Verificar obstáculos antes de moverse (solo si no ha cambiado demasiado)
            if(contadorCambiosDireccion < maxCambiosDireccion && DetectarObstaculo(direccion))
            {
                CambiarDireccion();
                return;
            }
            
            transform.position += direccion * velocidadMovimiento * Time.deltaTime;
            
            // Reducir contador si se está moviendo bien
            if(Vector3.Distance(transform.position, ultimaPosicion) > 0.1f)
            {
                contadorCambiosDireccion = Mathf.Max(0, contadorCambiosDireccion - 1);
            }

            if(direccion.sqrMagnitude > 0.0001f)
            {
                Quaternion rotObjetivo = Quaternion.LookRotation(direccion, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotObjetivo, velocidadRotacion * Time.deltaTime);
            }

            if(anim != null)
            {
                anim.SetFloat("VelX", 0f);
                anim.SetFloat("VelY", velocidadMovimiento);
            }

            // Detección de atasco: si casi no avanzó en un rato, cambia destino
            float avance = Vector3.Distance(transform.position, ultimaPosicion);
            if(avance < 0.02f)
            {
                tiempoSinProgreso += Time.deltaTime;
                if(tiempoSinProgreso >= tiempoMaxSinProgreso)
                {
                    // Si se atasca, intentar continuar hacia el destino actual
                    ReanudarPatrulla();
                    tiempoSinProgreso = 0f;
                }
            }
            else
            {
                tiempoSinProgreso = 0f;
            }

            ultimaPosicion = transform.position;
        }
    }

    // Reanuda la patrulla entre los puntos A y B
    private void ReanudarPatrulla()
    {
        // Determinar cuál punto está más cerca
        float distA = Vector3.Distance(transform.position, puntoA);
        float distB = Vector3.Distance(transform.position, puntoB);
        
        if(distA < distB)
        {
            destinoActual = puntoB;
            yendoHaciaB = true;
        }
        else
        {
            destinoActual = puntoA;
            yendoHaciaB = false;
        }
        tieneDestino = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if(!string.IsNullOrEmpty(jugadorTag) && other.CompareTag(jugadorTag) && !videoReproduciendo)
        {
            // Iniciar temporizador de captura
            capturando = true;
            tiempoCapturando = 0f;
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        // Si está dentro del trigger del jugador, contar tiempo de captura
        if(!string.IsNullOrEmpty(jugadorTag) && other.CompareTag(jugadorTag) && !videoReproduciendo)
        {
            if(!capturando)
            {
                capturando = true;
                tiempoCapturando = 0f;
                Debug.Log("¡Iniciando captura (trigger)! Tiempo requerido: " + tiempoCaptura + " segundos");
            }
            
            tiempoCapturando += Time.deltaTime;
            
            // Debug cada 0.2 segundos para ver el progreso
            if(Mathf.FloorToInt(tiempoCapturando * 5) != Mathf.FloorToInt((tiempoCapturando - Time.deltaTime) * 5))
            {
                Debug.Log($"Capturando (trigger)... Tiempo acumulado: {tiempoCapturando:F2}s / {tiempoCaptura}s");
            }
            
            // Si ha capturado al jugador durante el tiempo suficiente, mostrar video
            if(tiempoCapturando >= tiempoCaptura)
            {
                Debug.Log("¡TIEMPO COMPLETADO (trigger)! Llamando a ReproducirVideoCaptura()");
                ReproducirVideoCaptura();
                capturando = false;
                tiempoCapturando = 0f;
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if(!string.IsNullOrEmpty(jugadorTag) && other.CompareTag(jugadorTag))
        {
            // Si el jugador sale del trigger, resetear el temporizador
            capturando = false;
            tiempoCapturando = 0f;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Si choca con el jugador, iniciar temporizador de captura
        if(collision.gameObject != null && !string.IsNullOrEmpty(jugadorTag) && collision.gameObject.CompareTag(jugadorTag) && !videoReproduciendo)
        {
            // Verificar distancia y asegurar que estén cerca
            if(jugador != null)
            {
                float distancia = Vector3.Distance(transform.position, jugador.position);
                if(distancia <= 2.0f)
                {
                    capturando = true;
                    tiempoCapturando = 0f;
                    Debug.Log("¡Colisión detectada! Iniciando temporizador de captura. Distancia: " + distancia.ToString("F2"));
                }
            }
            else
            {
                capturando = true;
                tiempoCapturando = 0f;
                Debug.Log("¡Colisión detectada! Iniciando temporizador de captura.");
            }
            return;
        }
        
        // Si choca con algo que no sea el jugador, cambiar dirección
        if(collision.gameObject != null && 
           !collision.gameObject.CompareTag(jugadorTag) &&
           collision.gameObject.transform != transform &&
           !collision.gameObject.transform.IsChildOf(transform))
        {
            // Validar que no sea un trigger
            if(collision.collider != null && !collision.collider.isTrigger)
            {
                CambiarDireccion();
            }
        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        // Si está en contacto continuo con el jugador, contar tiempo de captura
        if(collision.gameObject != null && !string.IsNullOrEmpty(jugadorTag) && collision.gameObject.CompareTag(jugadorTag) && !videoReproduciendo)
        {
            // Verificar distancia más generosa para asegurar detección
            float distancia = Vector3.Distance(transform.position, jugador.position);
            // Distancia aumentada para captura más robusta (2.0f permite más margen)
            if(distancia <= 2.0f)
            {
                if(!capturando)
                {
                    capturando = true;
                    tiempoCapturando = 0f;
                    Debug.Log("¡Iniciando captura! Tiempo requerido: " + tiempoCaptura + " segundos. Distancia: " + distancia.ToString("F2"));
                }
                
                tiempoCapturando += Time.deltaTime;
                
                // Debug cada 0.2 segundos para ver el progreso
                if(Mathf.FloorToInt(tiempoCapturando * 5) != Mathf.FloorToInt((tiempoCapturando - Time.deltaTime) * 5))
                {
                    Debug.Log($"Capturando... Tiempo acumulado: {tiempoCapturando:F2}s / {tiempoCaptura}s (Distancia: {distancia:F2})");
                }
                
                // Si ha capturado al jugador durante el tiempo suficiente, mostrar video
                if(tiempoCapturando >= tiempoCaptura)
                {
                    Debug.Log("¡TIEMPO COMPLETADO! Llamando a ReproducirVideoCaptura()");
                    ReproducirVideoCaptura();
                    capturando = false;
                    tiempoCapturando = 0f;
                }
            }
            else
            {
                // Solo resetear si están MUY lejos (mayor margen para evitar resets accidentales)
                if(distancia > 3.0f && capturando)
                {
                    Debug.Log("Jugador se alejó mucho, reseteando captura. Distancia: " + distancia.ToString("F2"));
                    capturando = false;
                    tiempoCapturando = 0f;
                }
            }
            return;
        }
        
        // Si se queda pegado con algo que no sea el jugador, cambiar dirección
        if(collision.gameObject != null && 
           !collision.gameObject.CompareTag(jugadorTag) &&
           collision.gameObject.transform != transform &&
           !collision.gameObject.transform.IsChildOf(transform))
        {
            // Validar que no sea un trigger y que haya pasado suficiente tiempo
            if(collision.collider != null && 
               !collision.collider.isTrigger &&
               Time.time - tiempoUltimoChoque >= tiempoEntreCambiosDireccion)
            {
                CambiarDireccion();
            }
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        // Si el jugador sale del contacto, resetear el temporizador
        if(collision.gameObject != null && !string.IsNullOrEmpty(jugadorTag) && collision.gameObject.CompareTag(jugadorTag))
        {
            capturando = false;
            tiempoCapturando = 0f;
        }
    }
    
    // Detecta si hay un obstáculo en la dirección de movimiento
    private bool DetectarObstaculo(Vector3 direccion, bool esPersecucion = false)
    {
        // Distancia mínima más corta durante persecución para no interrumpir tanto
        float distanciaMinima = esPersecucion ? 0.5f : 0.3f;
        
        RaycastHit hit;
        Vector3 origen = transform.position + Vector3.up * 0.8f; // Más arriba para evitar suelo
        
        // Lanzar raycast ignorando triggers y el propio collider
        if(Physics.Raycast(origen, direccion, out hit, distanciaDeteccionObstaculos, capaObstaculos, QueryTriggerInteraction.Ignore))
        {
            // Validar que la distancia sea significativa (no el propio collider)
            if(hit.distance < distanciaMinima)
            {
                return false; // Muy cerca, probablemente es el propio collider
            }
            
            // Ignorar si es el propio collider del policía
            if(hit.collider == colliderPropio || hit.collider.transform.IsChildOf(transform))
            {
                return false;
            }
            
            // Ignorar si es el jugador
            if(!string.IsNullOrEmpty(jugadorTag) && hit.collider.CompareTag(jugadorTag))
            {
                return false;
            }
            
            // Validar que el objeto tenga un collider válido y no sea trigger
            if(hit.collider != null && !hit.collider.isTrigger)
            {
                // Verificar que el ángulo de impacto sea razonable (no demasiado bajo como suelo plano)
                float anguloImpacto = Vector3.Angle(Vector3.up, hit.normal);
                
                // Si el ángulo es muy bajo (casi horizontal), es probablemente una pared
                // Si es muy alto (casi vertical), probablemente es el suelo - ignorarlo si está lejos
                if(anguloImpacto > 75f && hit.distance > 1f)
                {
                    return false; // Probablemente es el suelo lejano
                }
                
                return true;
            }
        }
        
        return false;
    }
    
    // Cambia la dirección cuando detecta un obstáculo o choca
    private void CambiarDireccion()
    {
        if(Time.time - tiempoUltimoChoque < tiempoEntreCambiosDireccion)
        {
            return; // Evitar cambios repetidos muy rápidos
        }
        
        tiempoUltimoChoque = Time.time;
        contadorCambiosDireccion++;
        
        // Si ha cambiado demasiado de dirección, volver a la patrulla normal
        if(contadorCambiosDireccion >= maxCambiosDireccion)
        {
            ReanudarPatrulla();
            tiempoSinProgreso = 0f;
            return;
        }
        
        // Si está persiguiendo, busca dirección alternativa cerca
        if(persiguiendo)
        {
            Vector3 nuevaDireccion = ObtenerDireccionAlternativaSimple();
            destinoActual = transform.position + nuevaDireccion * 4f;
            destinoActual.y = transform.position.y;
            tieneDestino = true;
            return;
        }
        
        // Si está patrullando, volver a la patrulla normal entre A y B
        ReanudarPatrulla();
        tiempoSinProgreso = 0f; // Resetear contador de progreso
    }
    
    // Obtiene una dirección alternativa simple (sin recursión)
    private Vector3 ObtenerDireccionAlternativaSimple()
    {
        // Rotar 90 grados a izquierda o derecha aleatoriamente
        float anguloGiro = Random.value > 0.5f ? 90f : -90f;
        Vector3 direccionActual = transform.forward;
        Quaternion rotacion = Quaternion.Euler(0, anguloGiro + Random.Range(-30f, 30f), 0);
        Vector3 nuevaDireccion = rotacion * direccionActual;
        
        return nuevaDireccion.normalized;
    }
    
    // Verifica si el policía puede VER realmente al jugador (sin obstáculos en medio)
    private bool PuedeVerJugador()
    {
        if(jugador == null)
        {
            return false;
        }
        
        Vector3 origen = transform.position + Vector3.up * 0.8f;
        Vector3 destino = jugador.position + Vector3.up * 0.8f; // Apuntar a la altura del jugador
        Vector3 direccion = (destino - origen).normalized;
        float distancia = Vector3.Distance(origen, destino);
        
        RaycastHit hit;
        
        // Lanzar raycast para verificar si hay obstáculos entre el policía y el jugador
        if(Physics.Raycast(origen, direccion, out hit, distancia, capaVision, QueryTriggerInteraction.Ignore))
        {
            // Si el raycast golpea algo, verificar si es el jugador o un obstáculo
            if(hit.collider != null)
            {
                // Si golpea el jugador o un hijo del jugador, puede verlo
                if(hit.collider.CompareTag(jugadorTag) || 
                   hit.collider.transform == jugador || 
                   hit.collider.transform.IsChildOf(jugador))
                {
                    return true;
                }
                
                // Si golpea cualquier otra cosa (muro, obstáculo), NO puede verlo
                return false;
            }
        }
        
        // Si no golpea nada, asumir que puede verlo (aunque esto es raro)
        return true;
    }
    
    // Configura el sistema de video para la captura
    private void ConfigurarVideoCaptura()
    {
        // Buscar el objeto VideoAtrapada en la escena
        objetoVideo = GameObject.Find(nombreObjetoVideo);
        
        if(objetoVideo == null)
        {
            Debug.LogWarning($"No se encontró el objeto '{nombreObjetoVideo}' en la jerarquía. Creando uno nuevo.");
            CrearSistemaVideoDesdeCero();
            return;
        }
        
        // Buscar componentes en el objeto VideoAtrapada o sus hijos
        videoCanvas = objetoVideo.GetComponent<Canvas>();
        if(videoCanvas == null)
        {
            videoCanvas = objetoVideo.GetComponentInChildren<Canvas>();
        }
        
        videoPlayer = objetoVideo.GetComponent<VideoPlayer>();
        if(videoPlayer == null)
        {
            videoPlayer = objetoVideo.GetComponentInChildren<VideoPlayer>(true); // Buscar incluso si está deshabilitado
        }
        
        // Si encontramos el VideoPlayer, asegurarnos de que esté habilitado
        if(videoPlayer != null)
        {
            videoPlayer.enabled = true;
            if(videoPlayer.gameObject != null)
            {
                videoPlayer.gameObject.SetActive(true);
            }
            Debug.Log("VideoPlayer encontrado en VideoAtrapada y habilitado.");
        }
        
        videoDisplay = objetoVideo.GetComponent<RawImage>();
        if(videoDisplay == null)
        {
            videoDisplay = objetoVideo.GetComponentInChildren<RawImage>();
        }
        
        // Si falta el Canvas, crearlo en el objeto VideoAtrapada
        if(videoCanvas == null)
        {
            videoCanvas = objetoVideo.AddComponent<Canvas>();
            videoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            videoCanvas.sortingOrder = 999;
            CanvasScaler scaler = objetoVideo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            objetoVideo.AddComponent<GraphicRaycaster>();
        }
        
        // Si falta el VideoPlayer, crearlo como hijo
        if(videoPlayer == null)
        {
            GameObject videoPlayerObj = new GameObject("VideoPlayer");
            videoPlayerObj.transform.SetParent(objetoVideo.transform, false);
            videoPlayer = videoPlayerObj.AddComponent<VideoPlayer>();
            videoPlayer.enabled = true; // Asegurarse de que esté habilitado
        }
        
        // Si falta el RawImage, crearlo como hijo del Canvas
        if(videoDisplay == null)
        {
            GameObject imageObj = new GameObject("VideoDisplay");
            imageObj.transform.SetParent(objetoVideo.transform, false);
            videoDisplay = imageObj.AddComponent<RawImage>();
            RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            videoDisplay.color = Color.white;
        }
        
        // Configurar VideoPlayer si ya existe
        if(videoPlayer != null)
        {
            // Asegurarse de que el VideoPlayer esté habilitado
            videoPlayer.enabled = true;
            
            // Si el VideoPlayer ya tiene un clip asignado, mantenerlo
            if(videoPlayer.clip == null && videoCaptura != null)
            {
                videoPlayer.clip = videoCaptura;
            }
            
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.waitForFirstFrame = true; // Esperar el primer frame antes de mostrar
            
            // Crear RenderTexture si no existe
            if(videoPlayer.targetTexture == null)
            {
                RenderTexture renderTexture = new RenderTexture(1920, 1080, 0);
                videoPlayer.targetTexture = renderTexture;
                Debug.Log("RenderTexture creado para VideoPlayer");
            }
            
            // Conectar RenderTexture con RawImage
            if(videoDisplay != null && videoPlayer.targetTexture != null)
            {
                videoDisplay.texture = videoPlayer.targetTexture;
                Debug.Log("RenderTexture conectado a RawImage");
            }
        }
        
        // Cargar video desde Resources si no está asignado directamente
        if(videoCaptura == null && !string.IsNullOrEmpty(nombreVideo))
        {
            Debug.Log("Intentando cargar video desde Resources: " + nombreVideo);
            videoCaptura = Resources.Load<VideoClip>(nombreVideo);
            if(videoCaptura != null)
            {
                Debug.Log("Video cargado exitosamente desde Resources: " + nombreVideo);
            }
            else
            {
                Debug.LogWarning("No se encontró el video '" + nombreVideo + "' en Resources. Busca en: Assets/Resources/");
            }
        }
        
        if(videoCaptura != null && videoPlayer != null)
        {
            videoPlayer.clip = videoCaptura;
            Debug.Log("VideoClip asignado al VideoPlayer: " + videoCaptura.name);
        }
        else
        {
            Debug.LogWarning("No se encontró el video de captura. Asegúrate de asignarlo en el Inspector del Policía o colocarlo en Assets/Resources/AnimacionCaptura");
        }
        
        // Ocultar el objeto hasta que sea necesario
        objetoVideo.SetActive(false);
        
        // Configurar callback cuando termine el video
        videoPlayer.loopPointReached += OnVideoTerminado;
    }
    
    // Crea el sistema de video desde cero si no existe el objeto
    private void CrearSistemaVideoDesdeCero()
    {
        // Crear Canvas para mostrar el video
        objetoVideo = new GameObject(nombreObjetoVideo);
        objetoVideo.transform.SetParent(null);
        videoCanvas = objetoVideo.AddComponent<Canvas>();
        videoCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        videoCanvas.sortingOrder = 999;
        CanvasScaler scaler = objetoVideo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        objetoVideo.AddComponent<GraphicRaycaster>();
        
        // Crear RawImage para mostrar el video
        GameObject imageObj = new GameObject("VideoDisplay");
        imageObj.transform.SetParent(objetoVideo.transform, false);
        videoDisplay = imageObj.AddComponent<RawImage>();
        RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        videoDisplay.color = Color.white;
        
        // Crear VideoPlayer
        GameObject videoPlayerObj = new GameObject("VideoPlayer");
        videoPlayerObj.transform.SetParent(objetoVideo.transform, false);
        videoPlayer = videoPlayerObj.AddComponent<VideoPlayer>();
        
        // Configurar VideoPlayer
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        
        // Crear RenderTexture
        RenderTexture renderTexture = new RenderTexture(1920, 1080, 0);
        videoPlayer.targetTexture = renderTexture;
        videoDisplay.texture = renderTexture;
        
        // Cargar video desde Resources si no está asignado directamente
        if(videoCaptura == null && !string.IsNullOrEmpty(nombreVideo))
        {
            videoCaptura = Resources.Load<VideoClip>(nombreVideo);
        }
        
        if(videoCaptura != null)
        {
            videoPlayer.clip = videoCaptura;
        }
        else
        {
            Debug.LogWarning("No se encontró el video de captura. Asegúrate de asignarlo en el Inspector o colocarlo en la carpeta Resources.");
        }
        
        objetoVideo.SetActive(false);
        
        // Configurar callback cuando termine el video
        videoPlayer.loopPointReached += OnVideoTerminado;
    }
    
    // Reproduce el video de captura
    private void ReproducirVideoCaptura()
    {
        Debug.Log("=== INTENTANDO REPRODUCIR VIDEO ===");
        
        if(videoReproduciendo)
        {
            Debug.LogWarning("El video ya se está reproduciendo.");
            return;
        }
        
        if(videoPlayer == null)
        {
            Debug.LogError("VideoPlayer es null! No se puede reproducir el video.");
            return;
        }
        
        if(objetoVideo == null)
        {
            Debug.LogError("ObjetoVideo es null! No se puede mostrar el video.");
            return;
        }
        
        // Detener movimiento del policía y del jugador
        persiguiendo = false;
        tieneDestino = false;
        
        // Detener movimiento del jugador si es posible
        if(jugador != null)
        {
            MonoBehaviour[] scripts = jugador.GetComponents<MonoBehaviour>();
            foreach(MonoBehaviour script in scripts)
            {
                if(script != null && script.enabled)
                {
                    script.enabled = false;
                }
            }
        }
        
        // Verificar que el video esté asignado
        if(videoPlayer.clip == null)
        {
            Debug.LogWarning("VideoPlayer.clip es null! Intentando cargar video...");
            
            // Intentar cargar desde Resources si no está asignado
            if(videoCaptura == null && !string.IsNullOrEmpty(nombreVideo))
            {
                videoCaptura = Resources.Load<VideoClip>(nombreVideo);
                if(videoCaptura != null)
                {
                    Debug.Log("Video cargado desde Resources: " + nombreVideo);
                }
                else
                {
                    // Intentar cargar directamente desde la ruta del archivo
                    Debug.LogWarning("No se encontró en Resources. Intenta asignar el video 'AnimacionCaptura.mp4' directamente en el Inspector del objeto Policía.");
                }
            }
            
            // Asignar el video al VideoPlayer
            if(videoCaptura != null)
            {
                videoPlayer.clip = videoCaptura;
                Debug.Log("VideoClip asignado al VideoPlayer: " + videoCaptura.name);
            }
            else
            {
                Debug.LogError("No se pudo cargar el video. Asegúrate de asignar 'AnimacionCaptura.mp4' en el Inspector del objeto Policía.");
                StartCoroutine(OcultarDespuesDeDelay(1f));
                return;
            }
        }
        
        // Verificar RenderTexture
        if(videoPlayer.targetTexture == null)
        {
            Debug.LogWarning("RenderTexture no asignado, creando uno nuevo.");
            RenderTexture renderTexture = new RenderTexture(1920, 1080, 0);
            videoPlayer.targetTexture = renderTexture;
            if(videoDisplay != null)
            {
                videoDisplay.texture = renderTexture;
            }
        }
        
        // Verificar RawImage
        if(videoDisplay == null)
        {
            Debug.LogWarning("RawImage no encontrado, buscando en hijos.");
            videoDisplay = objetoVideo.GetComponentInChildren<RawImage>();
            if(videoDisplay != null && videoPlayer.targetTexture != null)
            {
                videoDisplay.texture = videoPlayer.targetTexture;
            }
        }
        
        // Asegurarse de que el objeto video y todos sus componentes estén activos ANTES de preparar
        objetoVideo.SetActive(true);
        
        // Asegurarse de que el GameObject del VideoPlayer esté activo
        if(videoPlayer.gameObject != null)
        {
            videoPlayer.gameObject.SetActive(true);
        }
        
        // Asegurarse de que el VideoPlayer esté habilitado
        if(!videoPlayer.enabled)
        {
            videoPlayer.enabled = true;
            Debug.Log("VideoPlayer estaba deshabilitado, habilitándolo ahora.");
        }
        
        // Esperar un frame para que Unity procese la activación
        StartCoroutine(EsperarActivacionYReproducir());
    }
    
    // Corrutina para esperar la activación y luego preparar y reproducir el video
    private IEnumerator EsperarActivacionYReproducir()
    {
        // Esperar un frame para que Unity procese la activación
        yield return null;
        
        // Verificar nuevamente que todo esté activo y habilitado
        if(objetoVideo != null)
        {
            objetoVideo.SetActive(true);
        }
        
        if(videoPlayer != null)
        {
            if(videoPlayer.gameObject != null)
            {
                videoPlayer.gameObject.SetActive(true);
            }
            
            if(!videoPlayer.enabled)
            {
                videoPlayer.enabled = true;
                yield return null; // Esperar otro frame después de habilitar
            }
            
            // Verificar que el VideoPlayer esté realmente habilitado ahora
            if(!videoPlayer.enabled)
            {
                Debug.LogError("No se pudo habilitar el VideoPlayer! Intentando forzar habilitación...");
                videoPlayer.enabled = true;
                yield return null; // Esperar otro frame
                yield return null; // Esperar otro frame más para asegurar
            }
            
            // Verificación final
            if(!videoPlayer.enabled)
            {
                Debug.LogError("CRÍTICO: El VideoPlayer no se puede habilitar. Verifica en el Inspector que el componente VideoPlayer esté activo.");
                videoReproduciendo = false;
                yield break;
            }
            
            Debug.Log("VideoPlayer habilitado correctamente. enabled=" + videoPlayer.enabled + ", GameObject activo=" + videoPlayer.gameObject.activeInHierarchy);
        }
        else
        {
            Debug.LogError("VideoPlayer es null!");
            videoReproduciendo = false;
            yield break;
        }
        
        videoReproduciendo = true;
        
        // Preparar el video
        Debug.Log("Preparando video para reproducción...");
        try
        {
            videoPlayer.Prepare();
        }
        catch(System.Exception e)
        {
            Debug.LogError("Error al preparar video: " + e.Message);
            videoReproduciendo = false;
            yield break;
        }
        
        // Esperar a que el video se prepare
        float tiempoEspera = 0f;
        while(videoPlayer != null && !videoPlayer.isPrepared && tiempoEspera < 5f)
        {
            tiempoEspera += Time.deltaTime;
            yield return null;
        }
        
        if(videoPlayer.isPrepared)
        {
            Debug.Log("Video preparado! Reproduciendo: " + (videoPlayer.clip != null ? videoPlayer.clip.name : "null"));
            videoPlayer.Play();
            
            // Verificar que se esté reproduciendo después de un momento
            yield return new WaitForSeconds(0.5f);
            if(!videoPlayer.isPlaying)
            {
                Debug.LogError("El video no se está reproduciendo después de prepararse. Intentando Play() de nuevo...");
                videoPlayer.Play();
            }
            else
            {
                Debug.Log("¡Video reproduciéndose correctamente!");
            }
        }
        else
        {
            Debug.LogError("No se pudo preparar el video después de 5 segundos. Verifica que el archivo de video esté en el formato correcto.");
            // Ocultar el objeto si no se puede reproducir
            objetoVideo.SetActive(false);
            videoReproduciendo = false;
        }
    }
    
    // Callback cuando el video termine
    private void OnVideoTerminado(VideoPlayer vp)
    {
        videoReproduciendo = false;
        if(videoPlayer != null)
        {
            videoPlayer.Stop();
        }
        if(objetoVideo != null)
        {
            objetoVideo.SetActive(false);
        }
        else if(videoCanvas != null)
        {
            videoCanvas.gameObject.SetActive(false);
        }
        
        // Rehabilitar movimiento del jugador
        if(jugador != null)
        {
            MonoBehaviour[] scripts = jugador.GetComponents<MonoBehaviour>();
            foreach(MonoBehaviour script in scripts)
            {
                if(script != null)
                {
                    script.enabled = true;
                }
            }
        }
        
        // Reactivar la patrulla del policía
        persiguiendo = false;
        ReanudarPatrulla();
        
        // NO cambiar de escena, solo ocultar el video
        // El juego continúa normalmente
    }
    
    // Corrutina para ocultar si no hay video
    private IEnumerator OcultarDespuesDeDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        videoReproduciendo = false;
        if(objetoVideo != null)
        {
            objetoVideo.SetActive(false);
        }
        else if(videoCanvas != null)
        {
            videoCanvas.gameObject.SetActive(false);
        }
        
        // Rehabilitar movimiento del jugador
        if(jugador != null)
        {
            MonoBehaviour[] scripts = jugador.GetComponents<MonoBehaviour>();
            foreach(MonoBehaviour script in scripts)
            {
                if(script != null)
                {
                    script.enabled = true;
                }
            }
        }
        
        // Reactivar la patrulla del policía
        persiguiendo = false;
        ReanudarPatrulla();
        
        // NO cambiar de escena, solo ocultar el video
    }
    
    void OnDestroy()
    {
        // Limpiar recursos del video
        if(videoPlayer != null && videoPlayer.targetTexture != null)
        {
            videoPlayer.targetTexture.Release();
        }
        
        if(videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoTerminado;
        }
    }
}


