export class LocalStorageUtils {

    //SESSION STORAGE
    public salvarUsuarioSession(user: string) {
        sessionStorage.setItem('user', JSON.stringify(user));
    }

    public salvarTokenUsuarioSession(token: string) {
        sessionStorage.setItem('token', token);
    }

    public obterTokenUsuarioSession(): string {
        return sessionStorage.getItem('token')!;
    }

    public obterUsuarioSession() {
        return JSON.parse(sessionStorage.getItem('user')!);
    }

    public obterUsuarioIdSession() {
        return JSON.parse(sessionStorage.getItem('user.id')!);
    }

    public salvarDadosLocaisUsuarioSession(response: any) {
        this.salvarTokenUsuarioSession(response.accessToken);
        this.salvarUsuarioSession(response.userToken);
    }

    public limparDadosLocaisUsuarioSession() {
        sessionStorage.removeItem('token');
        sessionStorage.removeItem('user');
    }

}