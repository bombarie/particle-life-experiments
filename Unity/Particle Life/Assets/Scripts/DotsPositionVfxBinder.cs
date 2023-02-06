using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

[AddComponentMenu("VFX/Property Binders/GPUDots/ParticleLife Dots Binder")]
[VFXBinder("GPUDots/DotsPositionData")]
sealed class DotsPositionVfxBinder : VFXBinderBase {
	public string Property {
		get => (string)_property;
		set => _property = value;
	}

	[VFXPropertyBinding("UnityEngine.GraphicsBuffer"), SerializeField]
	ExposedProperty _property = "DotsPositionsBuffer";

	public MainScript Source = null;

	public override bool IsValid (VisualEffect component)
		=> Source != null && component.HasGraphicsBuffer(_property);

	public override void UpdateBinding (VisualEffect component)
		=> component.SetGraphicsBuffer(_property, Source.GetDotsPositionBuffer());

	public override string ToString ()
		=> $"DotsPositionData : '{_property}' -> {Source?.name ?? "(null)"}";
}