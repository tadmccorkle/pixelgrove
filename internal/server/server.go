package server

import (
	"log/slog"
	"net/http"
	"net/http/httputil"
	"net/url"
	"os"

	"github.com/joho/godotenv"
)

type server struct {
	IsDev bool
}

func Serve() {
	err := godotenv.Load()
	if err != nil {
		slog.Error("failed to load .env", "error", err)
		os.Exit(1)
	}

	port := os.Getenv("PIXELGROVE_PORT")
	if port == "" {
		port = "4815"
	}

	var s server

	s.IsDev = os.Getenv("PIXELGROVE_ENV") == "dev"

	mux := http.NewServeMux()

	mux.HandleFunc("POST /login", handlerLogin)
	mux.HandleFunc("POST /logout", handlerLogout)

	if s.IsDev {
		webappPort := os.Getenv("PIXELGROVE_WEBAPP_DEV_PORT")
		if webappPort == "" {
			webappPort = "3000"
		}

		rp, err := url.Parse("http://localhost:" + webappPort)
		if err != nil {
			slog.Error("failed to set up client dev proxy", "error", err)
			os.Exit(1)
		}
		mux.Handle("/", httputil.NewSingleHostReverseProxy(rp))
	}

	server := &http.Server{
		Handler: mux,
		Addr:    ":" + port,
	}

	slog.Info("server running at http://localhost" + server.Addr)

	if err := server.ListenAndServe(); err != http.ErrServerClosed {
		slog.Error("HTTP server error", "error", err)
		os.Exit(1)
	}
}

func handlerLogin(w http.ResponseWriter, r *http.Request) {
	w.Header().Add("Content-Type", "application/json")
	w.WriteHeader(200)
	w.Write([]byte(`{ "user": "tad" }`))
}

func handlerLogout(w http.ResponseWriter, r *http.Request) {
	w.Header().Add("Content-Type", "application/json")
	w.WriteHeader(200)
	w.Write([]byte(`{ "user": "tad" }`))
}
