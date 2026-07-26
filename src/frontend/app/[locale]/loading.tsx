'use client';

export default function Loading() {
    return (
        <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 to-slate-100">
            <div className="text-center space-y-6 animate-fade-in">
                <div className="relative">
                    <div className="h-16 w-16 mx-auto rounded-2xl bg-gradient-to-br from-indigo-500 to-purple-600 shadow-xl shadow-indigo-500/30 animate-pulse" />
                    <div className="absolute inset-0 h-16 w-16 mx-auto rounded-2xl bg-gradient-to-br from-indigo-500 to-purple-600 opacity-30 animate-ping" />
                </div>
                <div className="space-y-2">
                    <h2
                        className="text-xl font-semibold text-slate-700"
                        style={{ fontFamily: 'Outfit, sans-serif' }}
                    >
                        Loading...
                    </h2>
                    <p className="text-sm text-slate-400">Setting things up for you</p>
                </div>
                <div className="flex justify-center gap-1.5">
                    {[0, 1, 2].map((i) => (
                        <div
                            key={i}
                            className="h-2 w-2 rounded-full bg-indigo-400"
                            style={{
                                animation: `bounce 1.4s infinite ease-in-out both`,
                                animationDelay: `${i * 0.16}s`,
                            }}
                        />
                    ))}
                </div>
            </div>
        </div>
    );
}
