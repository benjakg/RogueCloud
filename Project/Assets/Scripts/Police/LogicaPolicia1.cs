using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LogicaPolicia1 : MonoBehaviour
{
    public float velocidadMovimiento = 3.0f; // velocidad de patrulla
    public float velocidadRotacion = 200.0f;
    private Animator anim;

    // Patrulla aleatoria en todo el plano
    public float umbralLlegada = 0.2f;
    public float esperaEnPuntoSeg = 0.0f;

    private float temporizadorEspera;

    // Plano de referencia para calcular límites
    public Transform planoReferencia; // Asigna el Plane de la escena aquí
    public bool autogenerarPuntos = true;
    public float margenBorde = 0.5f; // margen para no salir del borde
    public float distanciaMinEntrePuntos = 2.0f;

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

        // Inicializar patrulla (elige un destino aleatorio si hay planoReferencia)
        if(autogenerarPuntos && planoReferencia != null)
        {
            destinoActual = GenerarPuntoAleatorioDentroDelPlano();
            tieneDestino = true;
        }
        else
        {
            // fallback: 5m hacia adelante
            destinoActual = transform.position + new Vector3(0f, 0f, 5f);
            tieneDestino = true;
        }
        temporizadorEspera = 0f;
        ultimaPosicion = transform.position;
        contadorCambiosDireccion = 0;
        tiempoUltimaVistaJugador = 0f;
    }

    void Update()
    {
        // Chequear detección de jugador (solo si está delante y puede VERLO)
        if(jugador != null)
        {
            float distJugador = Vector3.Distance(transform.position, jugador.position);
            
            // Verificar si el jugador está delante del policía
            Vector3 direccionAlJugador = (jugador.position - transform.position).normalized;
            Vector3 direccionFrente = transform.forward;
            
            // Calcular el ángulo entre la dirección del policía y hacia el jugador
            float angulo = Vector3.Angle(direccionFrente, direccionAlJugador);
            
            // Solo persigue si el jugador está dentro del radio Y delante (dentro del ángulo de visión)
            bool jugadorDelante = angulo <= anguloVision;
            
            // Verificar si realmente puede VER al jugador (sin obstáculos en medio)
            bool puedeVerJugador = PuedeVerJugador();
            
            if(!persiguiendo && distJugador <= radioDeteccion && jugadorDelante && puedeVerJugador)
            {
                // Solo empieza a perseguir si el jugador está delante Y puede verlo
                persiguiendo = true;
                tiempoUltimaVistaJugador = Time.time; // Resetear temporizador
            }
            else if(persiguiendo)
            {
                // Si está persiguiendo, verificar si todavía puede ver al jugador
                if(puedeVerJugador && jugadorDelante && distJugador <= radioPerdida)
                {
                    // Actualizar tiempo de última vista
                    tiempoUltimaVistaJugador = Time.time;
                }
                else
                {
                    // No puede ver al jugador o está fuera de rango
                    // Verificar si ha pasado el tiempo límite sin verlo
                    float tiempoSinVer = Time.time - tiempoUltimaVistaJugador;
                    
                    if(tiempoSinVer >= tiempoSinVerJugador || distJugador >= radioPerdida || !jugadorDelante)
                    {
                        // Dejar de perseguir después de 2 segundos sin ver o si se aleja/está detrás
                        persiguiendo = false;
                        tiempoUltimaVistaJugador = 0f;
                        if(planoReferencia != null)
                        {
                            destinoActual = GenerarPuntoAleatorioDentroDelPlano();
                        }
                        else
                        {
                            destinoActual = transform.position + Random.insideUnitSphere * 5f;
                            destinoActual.y = transform.position.y;
                        }
                        tieneDestino = true;
                    }
                }
            }
        }

        if(persiguiendo && jugador != null)
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

            return;
        }

        // Movimiento aleatorio por todo el plano
        if(tieneDestino)
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
                // Elegir nuevo destino aleatorio
                if(planoReferencia != null)
                {
                    destinoActual = GenerarPuntoAleatorioDentroDelPlano();
                }
                else
                {
                    // Si no hay planoReferencia, variar destino alrededor
                    destinoActual = transform.position + Random.insideUnitSphere * 5f;
                    destinoActual.y = transform.position.y;
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
                    if(planoReferencia != null)
                    {
                        destinoActual = GenerarPuntoAleatorioDentroDelPlano();
                    }
                    else
                    {
                        destinoActual = transform.position + Random.insideUnitSphere * 5f;
                        destinoActual.y = transform.position.y;
                    }
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
    
    // Sin inventario ni triggers: solo movimiento aleatorio

    // Genera un punto aleatorio dentro de los límites del Plane (10x10 escalado)
    private Vector3 GenerarPuntoAleatorioDentroDelPlano()
    {
        // El Plane de Unity mide 10 unidades por eje a escala 1
        float ancho = 10f * planoReferencia.localScale.x;
        float largo = 10f * planoReferencia.localScale.z;
        float mitadAncho = ancho * 0.5f - margenBorde;
        float mitadLargo = largo * 0.5f - margenBorde;

        Vector3 centro = planoReferencia.position;

        Vector3 punto;
        int intentos = 0;
        do
        {
            punto = new Vector3(
                Random.Range(-mitadAncho, mitadAncho),
                centro.y,
                Random.Range(-mitadLargo, mitadLargo)
            ) + new Vector3(centro.x, 0f, centro.z);
            intentos++;
        } while(Vector3.Distance(transform.position, punto) < distanciaMinEntrePuntos && intentos < 20);

        return punto;
    }

    void OnTriggerEnter(Collider other)
    {
        if(!string.IsNullOrEmpty(jugadorTag) && other.CompareTag(jugadorTag))
        {
            // Al colisionar con el jugador, volver al menú principal
            SceneManager.LoadScene("MainMenu");
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Si choca con el jugador, volver al menú principal
        if(collision.gameObject != null && !string.IsNullOrEmpty(jugadorTag) && collision.gameObject.CompareTag(jugadorTag))
        {
            SceneManager.LoadScene("MainMenu");
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
        // Si está en contacto continuo con el jugador, volver al menú principal
        if(collision.gameObject != null && !string.IsNullOrEmpty(jugadorTag) && collision.gameObject.CompareTag(jugadorTag))
        {
            SceneManager.LoadScene("MainMenu");
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
        
        // Si ha cambiado demasiado de dirección, generar un destino completamente aleatorio
        if(contadorCambiosDireccion >= maxCambiosDireccion)
        {
            if(planoReferencia != null)
            {
                destinoActual = GenerarPuntoAleatorioDentroDelPlano();
            }
            else
            {
                destinoActual = transform.position + Random.onUnitSphere * 8f;
                destinoActual.y = transform.position.y;
            }
            tieneDestino = true;
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
        
        // Si está patrullando, cambiar a un nuevo destino aleatorio
        if(planoReferencia != null)
        {
            destinoActual = GenerarPuntoAleatorioDentroDelPlano();
        }
        else
        {
            // Generar dirección aleatoria hacia un lado
            Vector3 direccionAleatoria = ObtenerDireccionAlternativaSimple();
            destinoActual = transform.position + direccionAleatoria * 5f;
            destinoActual.y = transform.position.y;
        }
        tieneDestino = true;
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
}


